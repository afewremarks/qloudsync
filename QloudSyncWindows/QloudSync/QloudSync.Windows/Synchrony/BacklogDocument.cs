using System;
using System.Xml;
using GreenQloud.Repository;
using System.IO;

namespace GreenQloud
{
    public class BacklogDocument : XmlDocument
    {
        private const string id = "id";
        private const string name = "name";
        private const string relativePath = "relativePath";
        private const string modificationDate = "modificationDate";
        private const string hash = "hash";

        public BacklogDocument ()
        {
            if (!System.IO.File.Exists (RuntimeSettings.BacklogFile))
                Create();
            Load(RuntimeSettings.BacklogFile);
        }

        public void Create (){
            if(!System.IO.File.Exists (RuntimeSettings.BacklogFile))
                System.IO.File.WriteAllText (RuntimeSettings.BacklogFile,
                                             "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n<files>\n</files>");
        }
        
        private void Save ()
        {
            
            if (!System.IO.File.Exists (RuntimeSettings.BacklogFile))
                throw new System.IO.FileNotFoundException (RuntimeSettings.BacklogFile + " does not exist");
            
            Save (RuntimeSettings.BacklogFile);
            
        }
        
        public void AddFile (StorageQloudObject file)
        {
            if (!System.IO.File.Exists (RuntimeSettings.BacklogFile)) {
                Create ();
                Load (RuntimeSettings.BacklogFile);
            }
            Logger.LogInfo ("Backlog", string.Format("Add {0} in backlog", file.FullLocalName));
            if (!EditFileByName(file))
            
            {                     
                XmlNode node_root = SelectSingleNode ("files");
                
                
                XmlNode node_id = CreateElement (id);
                XmlNode node_name = CreateElement (name);
                XmlNode node_relativePath = CreateElement (relativePath);
                XmlNode node_modificationDate = CreateElement (modificationDate);
                XmlNode node_hash = CreateElement (hash);

               
                try {
                    node_id.InnerText = (int.Parse (node_root.LastChild [id].InnerText) + 1).ToString();
                } catch {
                    node_id.InnerText = "1";
                }
                node_name.InnerText       = file.Name;
                node_relativePath.InnerText = file.RelativePath;
                node_modificationDate.InnerText        = file.TimeOfLastChange.ToString();
                if(file.IsAFolder)
                    node_hash.InnerText = "";
                else if (file.LocalMD5Hash == null)
                    node_hash.InnerText = "";
                else
                    node_hash.InnerText    = file.LocalMD5Hash.ToString();

                XmlNode node_file = CreateNode (XmlNodeType.Element, "file", null);
                
                node_file.AppendChild (node_id);
                node_file.AppendChild (node_name);
                node_file.AppendChild (node_relativePath);
                node_file.AppendChild (node_modificationDate);
                node_file.AppendChild (node_hash);
                
                node_root.AppendChild (node_file);
            }
            Save ();
        }
        
        public void RemoveAllFiles ()
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                SelectSingleNode ("files").RemoveChild (node_file);
            }
            
            Save ();
        }
        
        public void RemoveFileById (StorageQloudObject file)
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                if (node_file [id].InnerText == file.Id.ToString())
                    SelectSingleNode ("files").RemoveChild (node_file);
            }
            
            Save ();
        }
        
        public void RemoveFileByAbsolutePath (StorageQloudObject file)
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                if (node_file [name].InnerText == file.Name && node_file [relativePath].InnerText == file.RelativePath)
                    SelectSingleNode ("files").RemoveChild (node_file);
            }
            
            Save ();
        }
        
        public void RemoveFileByHash (StorageQloudObject file)
        {
            foreach (XmlNode node_file in SelectNodes ("files/file")) {
                if (node_file [hash].InnerText == file.LocalMD5Hash.ToString())
                                SelectSingleNode ("files").RemoveChild (node_file);
                }
                
                Save ();
        }
        
        public void EditFileById (StorageQloudObject file){
            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                if (node_file [id].InnerText == file.Id.ToString()){
                    UpdateFile (file, node_file);
                    continue;
                }
            }    
        }
        
        public void EditFileByHash (StorageQloudObject file)
        {
            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                if (node_file [hash].InnerText == file.LocalMD5Hash.ToString()){
                    UpdateFile (file, node_file);
                    continue;
                }
            }
        }
        
        public bool EditFileByName(StorageQloudObject file)
        {
            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                if (node_file [name].InnerText == file.Name.ToString() && node_file[relativePath].InnerText == file.RelativePath){
                    UpdateFile (file, node_file);
                    return true;
                }
            }
            return false;
        }
        
        public void UpdateFile(StorageQloudObject file, XmlNode node_file)
        {
                XmlNode node_name = node_file.SelectSingleNode (name);
                node_name.InnerText = file.Name;
                XmlNode node_relativePath = node_file.SelectSingleNode (relativePath);
                node_relativePath.InnerText = file.RelativePath;
                XmlNode node_modificationDate = node_file.SelectSingleNode (modificationDate);
                node_modificationDate.InnerText = file.TimeOfLastChange.ToString();
                XmlNode node_hash = node_file.SelectSingleNode (hash);
                if(file.IsAFolder)
                    node_hash.InnerText = "";
                else if (file.LocalMD5Hash == null)
                    node_hash.InnerText = "";
                else
                node_hash.InnerText    = file.LocalMD5Hash.ToString();
                Save ();
        }
        
        public StorageQloudObject Get (string absolutePath)
        {
            absolutePath = LocalRepo.ResolveDecodingProblem (absolutePath);

            foreach (XmlNode node_file in SelectNodes ("/files/file")) {
                string ap = string.Format("{0}{1}",node_file[relativePath].InnerText, node_file[name].InnerText);
                ap = LocalRepo.ResolveDecodingProblem (ap);
                if (absolutePath == ap || string.Format ("{0}{1}",absolutePath,Path.DirectorySeparatorChar) == ap)
                {
                    StorageQloudObject file = new StorageQloudObject (ap);
                    file.Id  = int.Parse(node_file[id].InnerText);
                    //file.LocalMD5Hash = node_file[hash].InnerText;
                    file.TimeOfLastChange = DateTime.Parse (node_file[modificationDate].InnerText);
                    return file;
                }
            }
            return null;
        }
    }
}

