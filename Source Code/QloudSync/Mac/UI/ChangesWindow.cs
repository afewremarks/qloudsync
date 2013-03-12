using System;
using MonoMac.AppKit;
using MonoMac.WebKit;
using System.Drawing;
using MonoMac.Foundation;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using GreenQloud.Repository;

namespace GreenQloud
{
    public class SparkleEventLog : NSWindow {
        
        public SparkleEventLogController Controller = new SparkleEventLogController ();
        public float TitlebarHeight;
        
        private WebView web_view;
        private NSBox background;
        private NSPopUpButton popup_button;
        private NSProgressIndicator progress_indicator;
        private NSTextField size_label, size_label_value, history_label, history_label_value;
        private NSButton hidden_close_button;
        
        
        public SparkleEventLog (IntPtr handle) : base (handle) { }
        
        public SparkleEventLog () : base ()
        {
            Title    = "Recent Changes";
            Delegate = new SparkleEventsDelegate ();
            
            int min_width  = 480;
            int min_height = 640;
            float x    = (float) (NSScreen.MainScreen.Frame.Width * 0.61);
            float y    = (float) (NSScreen.MainScreen.Frame.Height * 0.5 - (min_height * 0.5));
            
            SetFrame (
                new RectangleF (
                new PointF (x, y),
                new SizeF (min_width, (int) (NSScreen.MainScreen.Frame.Height * 0.85))),
                true);
            
            StyleMask = (NSWindowStyle.Closable | NSWindowStyle.Miniaturizable |
                         NSWindowStyle.Titled | NSWindowStyle.Resizable);
            
            MinSize        = new SizeF (min_width, min_height);
            HasShadow      = true;
            BackingType    = NSBackingStore.Buffered;
            TitlebarHeight = Frame.Height - ContentView.Frame.Height;
            
            
            this.web_view = new WebView (new RectangleF (0, 0, 481, 579), "", "") {
                Frame = new RectangleF (new PointF (0, 0),
                                        new SizeF (ContentView.Frame.Width, ContentView.Frame.Height - 39))
            };
            
            
            this.hidden_close_button = new NSButton () {
                KeyEquivalentModifierMask = NSEventModifierMask.CommandKeyMask,
                KeyEquivalent = "w"
            };
            
            this.hidden_close_button.Activated += delegate {
                Controller.WindowClosed ();
            };
            
            
            this.size_label = new NSTextField () {
                Alignment       = NSTextAlignment.Right,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (0, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)),
                StringValue     = "Size:",
                Font            = SparkleUI.BoldFont
            };
            
            this.size_label_value = new NSTextField () {
                Alignment       = NSTextAlignment.Left,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (60, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)),
                StringValue     = "…",
                Font            = SparkleUI.Font
            };
            
            
            this.history_label = new NSTextField () {
                Alignment       = NSTextAlignment.Right,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (130, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)),
                StringValue     = "History:",
                Font            = SparkleUI.BoldFont
            };
            
            this.history_label_value = new NSTextField () {
                Alignment       = NSTextAlignment.Left,
                BackgroundColor = NSColor.WindowBackground,
                Bordered        = false,
                Editable        = false,
                Frame           = new RectangleF (
                    new PointF (190, ContentView.Frame.Height - 30),
                    new SizeF (60, 20)
                    ),
                StringValue     = "…",
                Font            = SparkleUI.Font
            };
            
            this.popup_button = new NSPopUpButton () {
                Frame = new RectangleF (
                    new PointF (ContentView.Frame.Width - 156 - 12, ContentView.Frame.Height - 33),
                    new SizeF (156, 26)),
                PullsDown = false
            };
            
            this.background = new NSBox () {
                Frame = new RectangleF (
                    new PointF (-1, -1),
                    new SizeF (Frame.Width + 2, this.web_view.Frame.Height + 2)),
                FillColor = NSColor.White,
                BorderColor = NSColor.LightGray,
                BoxType = NSBoxType.NSBoxCustom
            };
            
            this.progress_indicator = new NSProgressIndicator () {
                Frame = new RectangleF (
                    new PointF (Frame.Width / 2 - 10, this.web_view.Frame.Height / 2 + 10),
                    new SizeF (20, 20)),
                Style = NSProgressIndicatorStyle.Spinning
            };
            
            this.progress_indicator.StartAnimation (this);
            
            ContentView.AddSubview (this.size_label);
            ContentView.AddSubview (this.size_label_value);
            ContentView.AddSubview (this.history_label);
            ContentView.AddSubview (this.history_label_value);
            ContentView.AddSubview (this.popup_button);
            ContentView.AddSubview (this.progress_indicator);
            ContentView.AddSubview (this.background);
            ContentView.AddSubview (this.hidden_close_button);
            
            (Delegate as SparkleEventsDelegate).WindowResized += delegate (SizeF new_window_size) {
                InvokeOnMainThread (() => Relayout (new_window_size));
            };
            
            
            // Hook up the controller events
            Controller.HideWindowEvent += delegate {
                InvokeOnMainThread (() => {
                    this.progress_indicator.Hidden = true;
                    PerformClose (this);
                });
            };
            
            Controller.ShowWindowEvent += delegate {
                InvokeOnMainThread (() => OrderFrontRegardless ());
            };
            
            Controller.UpdateChooserEvent += delegate (string [] folders) {
                InvokeOnMainThread (() => UpdateChooser (folders));
            };
            
            Controller.UpdateChooserEnablementEvent += delegate (bool enabled) {
                InvokeOnMainThread (() => { this.popup_button.Enabled = enabled; });
            };
            
            Controller.UpdateContentEvent += delegate (string html) {
                InvokeOnMainThread (() => {
                    this.progress_indicator.Hidden = true;
                    UpdateContent (html);
                });
            };
            
            Controller.ContentLoadingEvent += delegate {
                InvokeOnMainThread (() => {
                    this.web_view.RemoveFromSuperview ();
                    this.progress_indicator.Hidden = false;
                    this.progress_indicator.StartAnimation (this);
                });
            };
            
            Controller.UpdateSizeInfoEvent += delegate (string size, string history_size) {
                InvokeOnMainThread (() => {
                    this.size_label_value.StringValue    = size;
                    this.history_label_value.StringValue = history_size;
                });
            };
            
            Controller.ShowSaveDialogEvent += delegate (string file_name, string target_folder_path) {
                InvokeOnMainThread (() => {
                    NSSavePanel panel = new NSSavePanel () {
                        DirectoryUrl         = new NSUrl (target_folder_path, true),
                        NameFieldStringValue = file_name,
                        ParentWindow         = this,
                        Title                = "Restore from History",
                        PreventsApplicationTerminationWhenModal = false
                    };
                    
                    if ((NSPanelButtonType) panel.RunModal () == NSPanelButtonType.Ok) {
                        string target_file_path = Path.Combine (panel.DirectoryUrl.RelativePath, panel.NameFieldStringValue);
                        Controller.SaveDialogCompleted (target_file_path);
                        
                    } else {
                        Controller.SaveDialogCancelled ();
                    }
                });
            };
        }
        
        
        public void Relayout (SizeF new_window_size)
        {
            this.web_view.Frame = new RectangleF (this.web_view.Frame.Location,
                                                  new SizeF (new_window_size.Width, new_window_size.Height - TitlebarHeight - 39));
            
            this.background.Frame = new RectangleF (this.background.Frame.Location,
                                                    new SizeF (new_window_size.Width, new_window_size.Height - TitlebarHeight - 37));
            
            this.size_label.Frame = new RectangleF (
                new PointF (this.size_label.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.size_label.Frame.Size);
            
            this.size_label_value.Frame = new RectangleF (
                new PointF (this.size_label_value.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.size_label_value.Frame.Size);
            
            this.history_label.Frame = new RectangleF (
                new PointF (this.history_label.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.history_label.Frame.Size);
            
            this.history_label_value.Frame = new RectangleF (
                new PointF (this.history_label_value.Frame.X, new_window_size.Height - TitlebarHeight - 30),
                this.history_label_value.Frame.Size);
            
            this.progress_indicator.Frame = new RectangleF (
                new PointF (new_window_size.Width / 2 - 10, this.web_view.Frame.Height / 2 + 10),
                this.progress_indicator.Frame.Size);
            
            this.popup_button.RemoveFromSuperview (); // Needed to prevent redraw glitches
            
            this.popup_button.Frame = new RectangleF (
                new PointF (new_window_size.Width - this.popup_button.Frame.Width - 12, new_window_size.Height - TitlebarHeight - 33),
                this.popup_button.Frame.Size);
            
            ContentView.AddSubview (this.popup_button);
        }
        
        
        public void UpdateChooser (string [] folders)
        {
           // if (folders == null)
             //   folders = Controller.Folders;
            
            this.popup_button.Cell.ControlSize = NSControlSize.Small;
            this.popup_button.Font = NSFontManager.SharedFontManager.FontWithFamily (
                "Lucida Grande", NSFontTraitMask.Condensed, 0, NSFont.SmallSystemFontSize);
            
            this.popup_button.RemoveAllItems ();
            
            this.popup_button.AddItem ("Summary");
            this.popup_button.Menu.AddItem (NSMenuItem.SeparatorItem);
            
            int row = 2;
            foreach (string folder in folders) {
                this.popup_button.AddItem (folder);
                
                if (folder.Equals (Controller.SelectedFolder))
                    this.popup_button.SelectItem (row);
                
                row++;
            }
            
            this.popup_button.AddItems (folders);
            
            this.popup_button.Activated += delegate {
                InvokeOnMainThread (() => {
                    if (this.popup_button.IndexOfSelectedItem == 0)
                        Controller.SelectedFolder = null;
                    else
                        Controller.SelectedFolder = this.popup_button.SelectedItem.Title;
                });
            };
        }
        
        
        public void UpdateContent (string html)
        {
            string pixmaps_path = "file://" + NSBundle.MainBundle.ResourcePath;
            
            html = html.Replace ("<!-- $body-font-family -->", "Lucida Grande");
            html = html.Replace ("<!-- $day-entry-header-font-size -->", "13.6px");
            html = html.Replace ("<!-- $body-font-size -->", "13.4px");
            html = html.Replace ("<!-- $secondary-font-color -->", "#bbb");
            html = html.Replace ("<!-- $small-color -->", "#ddd");
            html = html.Replace ("<!-- $small-font-size -->", "10px");
            html = html.Replace ("<!-- $day-entry-header-background-color -->", "#f5f5f5");
            html = html.Replace ("<!-- $a-color -->", "#0085cf");
            html = html.Replace ("<!-- $a-hover-color -->", "#009ff8");
            html = html.Replace ("<!-- $pixmaps-path -->", pixmaps_path);
            html = html.Replace ("<!-- $document-added-background-image -->", pixmaps_path + "/document-added-12.png");
            html = html.Replace ("<!-- $document-deleted-background-image -->", pixmaps_path + "/document-deleted-12.png");
            html = html.Replace ("<!-- $document-edited-background-image -->", pixmaps_path + "/document-edited-12.png");
            html = html.Replace ("<!-- $document-moved-background-image -->", pixmaps_path + "/document-moved-12.png");
            
            this.web_view = new WebView (new RectangleF (0, 0, 481, 579), "", "") {
                Frame = new RectangleF (new PointF (0, 0), new SizeF (ContentView.Frame.Width, ContentView.Frame.Height - 39))
            };
            
            this.web_view.MainFrame.LoadHtmlString (html, new NSUrl (""));
            
            /*this.web_view.PolicyDelegate = new SparkleWebPolicyDelegate ();
            ContentView.AddSubview (this.web_view);
            
            (this.web_view.PolicyDelegate as SparkleWebPolicyDelegate).LinkClicked += delegate (string href) {
                if (href.StartsWith ("file:///"))
                    href = href.Substring (7);
                
                Controller.LinkClicked (href);
            };*/
            
            this.progress_indicator.Hidden = true;
        }
        
        
        public override void OrderFrontRegardless ()
        {
            NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
            MakeKeyAndOrderFront (this);
            
            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();
            
            base.OrderFrontRegardless ();
        }
        
        
        public override void PerformClose (NSObject sender)
        {
            base.OrderOut (this);
            
            if (Program.UI != null)
                Program.UI.UpdateDockIconVisibility ();
            
            return;
        }
    }
    
    
    public class SparkleEventsDelegate : NSWindowDelegate {
        
        public event WindowResizedHandler WindowResized = delegate { };
        public delegate void WindowResizedHandler (SizeF new_window_size);
        
        public override SizeF WillResize (NSWindow sender, SizeF to_frame_size)
        {
            WindowResized (to_frame_size);
            return to_frame_size;
        }
        
        public override bool WindowShouldClose (NSObject sender)
        {
            (sender as SparkleEventLog).Controller.WindowClosed ();
            return false;
        }
    }

    public class SparkleEventLogController {
        
        public event Action ShowWindowEvent = delegate { };
        public event Action HideWindowEvent = delegate { };
        public event Action ContentLoadingEvent = delegate { };
        
        public event UpdateContentEventEventHandler UpdateContentEvent = delegate { };
        public delegate void UpdateContentEventEventHandler (string html);
        
        public event UpdateChooserEventHandler UpdateChooserEvent = delegate { };
        public delegate void UpdateChooserEventHandler (string [] folders);
        
        public event UpdateChooserEnablementEventHandler UpdateChooserEnablementEvent = delegate { };
        public delegate void UpdateChooserEnablementEventHandler (bool enabled);
        
        public event UpdateSizeInfoEventHandler UpdateSizeInfoEvent = delegate { };
        public delegate void UpdateSizeInfoEventHandler (string size, string history_size);
        
        public event ShowSaveDialogEventHandler ShowSaveDialogEvent = delegate { };
        public delegate void ShowSaveDialogEventHandler (string file_name, string target_folder_path);
        
        
        private string selected_folder;
        //private RevisionInfo restore_revision_info;
        private bool history_view_active;
        
        
        public bool WindowIsOpen { get; private set; }
        
        public string SelectedFolder {
            get {
                return this.selected_folder;
            }
            
            set {
                this.selected_folder = value;
                
                ContentLoadingEvent ();
                UpdateSizeInfoEvent ("…", "…");
                
                new Thread (() => {
                    //SparkleDelay delay = new SparkleDelay ();
                    string html = HTML;
                    //delay.Stop ();
                    
                    if (!string.IsNullOrEmpty (html))
                        UpdateContentEvent (html);
                    
                    UpdateSizeInfoEvent (Size, HistorySize);
                    
                }).Start ();
            }
        }
        
        public string HTML {
            get {
                //List<SparkleChangeSet> change_sets = GetLog (this.selected_folder);
                string html = "";//GetHTMLLog (change_sets);
                
                return html;
            }
        }
        
        /*public string [] Folders {
            get {
                return Program.Controller.Folders.ToArray ();
            }
        }*/
        
        public string Size {
            get {
                double size = 0;
                
               /* foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                    if (this.selected_folder == null) {
                        size += repo.Size;
                        
                    } else if (this.selected_folder.Equals (repo.Name)) {
                        if (repo.Size == 0)
                            return "???";
                        else
                            return repo.Size.ToSize ();
                    }
                }*/
                
                if (size == 0)
                    return "???";
                else
                    return "";//size.ToSize ();
            }
        }
        
        public string HistorySize {
            get {
                double size = 0;
                
                /*foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                    if (this.selected_folder == null) {
                        size += repo.HistorySize;
                        
                    } else if (this.selected_folder.Equals (repo.Name)) {
                        if (repo.HistorySize == 0)
                            return "???";
                        else
                            return repo.HistorySize.ToSize ();
                    }
                }*/
                
                if (size == 0)
                    return "???";
                else
                    return "";//size.ToSize ();
            }
        }
        
        
        public SparkleEventLogController ()
        {
            Program.Controller.ShowEventLogWindowEvent += delegate {

                if (!WindowIsOpen) {
                    ContentLoadingEvent ();
                    UpdateSizeInfoEvent ("…", "…");
                    
                    if (this.selected_folder == null) {
                        new Thread (() => {
                            //SparkleDelay delay = new SparkleDelay ();
                            string html = HTML;
                            //delay.Stop ();
                            
                            //UpdateChooserEvent (Folders);
                            UpdateChooserEnablementEvent (true);
                            
                            if (!string.IsNullOrEmpty (html))
                                UpdateContentEvent (html);
                            
                            UpdateSizeInfoEvent (Size, HistorySize);
                            
                        }).Start ();
                    }
                }
                
                WindowIsOpen = true;
                ShowWindowEvent ();
            };
            
            Program.Controller.OnIdle += delegate {
                if (this.history_view_active)
                    return;
                
                ContentLoadingEvent ();
                UpdateSizeInfoEvent ("…", "…");
                
                //SparkleDelay delay = new SparkleDelay ();
                string html = HTML;
                //delay.Stop ();
                
                if (!string.IsNullOrEmpty (html))
                    UpdateContentEvent (html);
                
                UpdateSizeInfoEvent (Size, HistorySize);
            };
            
           /* Program.Controller.FolderListChanged += delegate {
                if (this.selected_folder != null && !Program.Controller.Folders.Contains (this.selected_folder))
                    this.selected_folder = null;
                
                UpdateChooserEvent (Folders);
                UpdateSizeInfoEvent (Size, HistorySize);
            };*/
        }
        
        
        public void WindowClosed ()
        {
            WindowIsOpen = false;
            HideWindowEvent ();
            this.selected_folder = null;
        }
        
        
        public void LinkClicked (string url)
        {
            if (url.StartsWith ("about:") || string.IsNullOrEmpty (url))
                return;
            
            url = url.Replace ("%20", " ");
            
            if (url.StartsWith ("http")) {
                Program.Controller.OpenWebsite (url);
                
            } 
//            else if (url.StartsWith ("restore://") && this.restore_revision_info == null) {
//                Regex regex = new Regex ("restore://(.+)/([a-f0-9]+)/(.+)/(.{3} [0-9]+ [0-9]+h[0-9]+)/(.+)");
//                Match match = regex.Match (url);
//                
//                if (match.Success) {
//                    string author_name = match.Groups [3].Value;
//                    string timestamp   = match.Groups [4].Value;
//                    
//                    this.restore_revision_info = new RevisionInfo () {
//                       // Folder   = new SparkleFolder (match.Groups [1].Value),
//                        Revision = match.Groups [2].Value,
//                        FilePath = match.Groups [5].Value
//                    };
//                    
//                    string file_name = Path.GetFileNameWithoutExtension (this.restore_revision_info.FilePath) +
//                        " (" + author_name + " " + timestamp + ")" + Path.GetExtension (this.restore_revision_info.FilePath);
//                    
//                    string target_folder_path = Path.Combine (this.restore_revision_info.Folder.FullPath,
//                                                              Path.GetDirectoryName (this.restore_revision_info.FilePath));
//                    
//                    ShowSaveDialogEvent (file_name, target_folder_path);
//                }
//                
//            } 
            else if (url.StartsWith ("back://")) {
                this.history_view_active = false;
                SelectedFolder           = this.selected_folder; // TODO: Return to the same position on the page
                
                UpdateChooserEnablementEvent (true);
                
            } else if (url.StartsWith ("history://")) {
                this.history_view_active = true;
                
                ContentLoadingEvent ();
                UpdateSizeInfoEvent ("…", "…");
                UpdateChooserEnablementEvent (false);
                
                string folder    = url.Replace ("history://", "").Split ("/".ToCharArray ()) [0];
                string file_path = url.Replace ("history://" + folder + "/", "");
                
                byte [] file_path_bytes = Encoding.Default.GetBytes (file_path);
                file_path = Encoding.UTF8.GetString (file_path_bytes);
//                
//                foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
//                    if (!repo.Name.Equals (folder))
//                        continue;
//                    
//                    new Thread (() => {
//                        //SparkleDelay delay = new SparkleDelay ();
//                        //List<SparkleChangeSet> change_sets = repo.GetChangeSets (file_path);
//                        //string html = GetHistoryHTMLLog (change_sets, file_path);
//                        //delay.Stop ();
//                        
//                        if (!string.IsNullOrEmpty (html))
//                            UpdateContentEvent (html);
//                        
//                    }).Start ();
//                    
//                    break;
//                }
//                
            } else {
                //Program.Controller.OpenFile (url);
            }   
        }
        
        
        public void SaveDialogCompleted (string target_file_path)
        {
       /*     foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                if (repo.Name.Equals (this.restore_revision_info.Folder.Name)) {
                    repo.RestoreFile (this.restore_revision_info.FilePath,
                                      this.restore_revision_info.Revision, target_file_path);
                    
                    break;
                }
            }
            
            this.restore_revision_info = null;
            Program.Controller.OpenFolder (Path.GetDirectoryName (target_file_path));*/
        }
        
        
        public void SaveDialogCancelled ()
        {
            //this.restore_revision_info = null;
        }
        
        
        /*private List<SparkleChangeSet> GetLog ()
        {
            List<SparkleChangeSet> list = new List<SparkleChangeSet> ();
            
            foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                List<SparkleChangeSet> change_sets = repo.ChangeSets;
                
                if (change_sets != null)
                    list.AddRange (change_sets);
                else
                    SparkleLogger.LogInfo ("Log", "Could not create log for " + repo.Name);
            }
            
            list.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
            list.Reverse ();
            
            if (list.Count > 100)
                return list.GetRange (0, 100);
            else
                return list.GetRange (0, list.Count);
        }
        
        
        private List<SparkleChangeSet> GetLog (string name)
        {
            if (name == null)
                return GetLog ();
            
            foreach (SparkleRepoBase repo in Program.Controller.Repositories) {
                if (repo.Name.Equals (name)) {
                    List<SparkleChangeSet> change_sets = repo.ChangeSets;
                    
                    if (change_sets != null)
                        return change_sets;
                    else
                        break;
                }
            }
            
            return new List<SparkleChangeSet> ();
        }
        */
        
        public string GetHistoryHTMLLog (List<Change> change_sets, string file_path)
        {
            string html = "<div class='history-header'>" +
                "<a class='windows' href='back://'>&laquo; Back</a> &nbsp;|&nbsp; ";
            
            if (change_sets.Count > 1)
                html += "Revisions for <b>&ldquo;";
            else
                html += "No revisions for <b>&ldquo;";
            
            html += Path.GetFileName (file_path) + "&rdquo;</b>";
            html += "</div><div class='table-wrapper'><table>";
            
            int count = 0;
            foreach (Change change_set in change_sets) {
                count++;
                
                if (count == 1)
                    continue;
                
                //string change_set_avatar = Program.Controller.GetAvatar (change_set.User.Email, 24);
                
//                if (change_set_avatar != null)
//                    change_set_avatar = "file://" + change_set_avatar.Replace ("\\", "/");
//                else
//                    change_set_avatar = "file://<!-- $pixmaps-path -->/user-icon-default.png";
//                
                html += "<tr>" +
                    "<td class='avatar'>"+//<img src='" + change_set_avatar + "'></td>" +
                        "<td class='name'>Angelo</td>" +
                        "<td class='date'>" + 
                        //change_set.Timestamp.ToString ("d MMM yyyy", CultureInfo.InvariantCulture) + 
                        "</td>" +
                        "<td class='time'>00:00</td>" +
                        "<td class='restore'>" +
                        "</td>" +
                        "</tr>";
                
                count++;
            }
            
            html += "</table></div>";
            //html = Program.Controller.EventLogHTML.Replace ("<!-- $event-log-content -->", html);
            
            return html.Replace ("<!-- $midnight -->", "100000000");
        }
        
        
        public string GetHTMLLog (List<Change> change_sets)
        {
            if (change_sets.Count == 0)
                return ""; // TODO "Project does not have a history"
            
//            List <ActivityDay> activity_days = new List <ActivityDay> ();
//            
//            change_sets.Sort ((x, y) => (x.Timestamp.CompareTo (y.Timestamp)));
//            change_sets.Reverse ();
//            
//            foreach (SparkleChangeSet change_set in change_sets) {
//                bool change_set_inserted = false;
//                
//                foreach (ActivityDay stored_activity_day in activity_days) {
//                    if (stored_activity_day.Date.Year  == change_set.Timestamp.Year &&
//                        stored_activity_day.Date.Month == change_set.Timestamp.Month &&
//                        stored_activity_day.Date.Day   == change_set.Timestamp.Day) {
//                        
//                        stored_activity_day.Add (change_set);
//                        
//                        change_set_inserted = true;
//                        break;
//                    }
//                }
//                
//                if (!change_set_inserted) {
//                    ActivityDay activity_day = new ActivityDay (change_set.Timestamp);
//                    activity_day.Add (change_set);
//                    activity_days.Add (activity_day);
//                }
//            }
//            
//            string event_log_html   = Program.Controller.EventLogHTML;
//            string day_entry_html   = Program.Controller.DayEntryHTML;
//            string event_entry_html = Program.Controller.EventEntryHTML;
//            string event_log        = "";
//            
//            foreach (ActivityDay activity_day in activity_days) {
//                string event_entries = "";
//                
//                foreach (SparkleChangeSet change_set in activity_day) {
//                    string event_entry = "<dl>";
//                    
//                    foreach (SparkleChange change in change_set.Changes) {
//                        if (change.Type != SparkleChangeType.Moved) {
//                            event_entry += "<dd class='" + change.Type.ToString ().ToLower () + "'>";
//                            
//                            if (!change.IsFolder) {
//                                event_entry += "<small><a href=\"history://" + change_set.Folder.Name + "/" + 
//                                    change.Path + "\" title=\"View revisions\">" + change.Timestamp.ToString ("HH:mm") +
//                                        "</a></small> &nbsp;";
//                                
//                            } else {
//                                event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") + "</small> &nbsp;";
//                            }
//                            
//                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
//                            event_entry += "</dd>";
//                            
//                        } else {
//                            event_entry += "<dd class='moved'>";
//                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
//                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.Path);
//                            event_entry += "<br>";
//                            event_entry += "<small>" + change.Timestamp.ToString ("HH:mm") +"</small> &nbsp;";
//                            event_entry += FormatBreadCrumbs (change_set.Folder.FullPath, change.MovedToPath);
//                            event_entry += "</dd>";
//                        }
//                    }
//                    
//                    string change_set_avatar = Program.Controller.GetAvatar (change_set.User.Email, 48);
//                    
//                    if (change_set_avatar != null)
//                        change_set_avatar = "file://" + change_set_avatar.Replace ("\\", "/");
//                    else
//                        change_set_avatar = "file://<!-- $pixmaps-path -->/user-icon-default.png";
//                    
//                    event_entry += "</dl>";
//                    
//                    string timestamp = change_set.Timestamp.ToString ("H:mm");
//                    
//                    if (!change_set.FirstTimestamp.Equals (new DateTime ()) &&
//                        !change_set.Timestamp.ToString ("H:mm").Equals (change_set.FirstTimestamp.ToString ("H:mm"))) {
//                        
//                        timestamp = change_set.FirstTimestamp.ToString ("H:mm") + " – " + timestamp;
//                    }
//                    
//                    event_entries += event_entry_html.Replace ("<!-- $event-entry-content -->", event_entry)
//                        .Replace ("<!-- $event-user-name -->", change_set.User.Name)
//                            .Replace ("<!-- $event-user-email -->", change_set.User.Email)
//                            .Replace ("<!-- $event-avatar-url -->", change_set_avatar)
//                            .Replace ("<!-- $event-url -->", change_set.RemoteUrl.ToString ())
//                            .Replace ("<!-- $event-revision -->", change_set.Revision);
//                    
//                    if (this.selected_folder == null) 
//                        event_entries = event_entries.Replace ("<!-- $event-folder -->", " @ " + change_set.Folder.Name);
//                }
//                
//                string day_entry   = "";
//                DateTime today     = DateTime.Now;
//                DateTime yesterday = DateTime.Now.AddDays (-1);
//                
//                if (today.Day   == activity_day.Date.Day &&
//                    today.Month == activity_day.Date.Month &&
//                    today.Year  == activity_day.Date.Year) {
//                    
//                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
//                                                        "<span id='today' name='" +
//                                                        activity_day.Date.ToString ("dddd, MMMM d", CultureInfo.InvariantCulture) + "'>" + "Today" +
//                                                        "</span>");
//                    
//                } else if (yesterday.Day   == activity_day.Date.Day &&
//                           yesterday.Month == activity_day.Date.Month &&
//                           yesterday.Year  == activity_day.Date.Year) {
//                    
//                    day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
//                                                        "<span id='yesterday' name='" + activity_day.Date.ToString ("dddd, MMMM d", CultureInfo.InvariantCulture) + "'>" +
//                                                        "Yesterday" +
//                                                        "</span>");
//                    
//                } else {
//                    if (activity_day.Date.Year != DateTime.Now.Year) {
//                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
//                                                            activity_day.Date.ToString ("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture));
//                        
//                    } else {
//                        day_entry = day_entry_html.Replace ("<!-- $day-entry-header -->",
//                                                            activity_day.Date.ToString ("dddd, MMMM d", CultureInfo.InvariantCulture));
//                    }
//                }
//                
//                event_log += day_entry.Replace ("<!-- $day-entry-content -->", event_entries);
//            }
//            
//            int midnight = (int) (DateTime.Today.AddDays (1) - new DateTime (1970, 1, 1)).TotalSeconds;
//            
//            string html = event_log_html.Replace ("<!-- $event-log-content -->", event_log);
//            html = html.Replace ("<!-- $midnight -->", midnight.ToString ());
//            
            return "";//html;
        }
        
        
        private string FormatBreadCrumbs (string path_root, string path)
        {
            byte [] path_root_bytes = Encoding.Default.GetBytes (path_root);
            byte [] path_bytes      = Encoding.Default.GetBytes (path);
            path_root               = Encoding.UTF8.GetString (path_root_bytes);
            path                    = Encoding.UTF8.GetString (path_bytes);
            
            path_root                = path_root.Replace ("/", Path.DirectorySeparatorChar.ToString ());
            path                     = path.Replace ("/", Path.DirectorySeparatorChar.ToString ());
            string new_path_root     = path_root;
            string [] crumbs         = path.Split (Path.DirectorySeparatorChar);
            string link              = "";
            bool previous_was_folder = false;
            
            int i = 0;
            foreach (string crumb in crumbs) {
                if (string.IsNullOrEmpty (crumb))
                    continue;
                
                string crumb_path = SafeCombine (new_path_root, crumb);
                
                if (Directory.Exists (crumb_path)) {
                    link += "<a href='" + crumb_path + "'>" + crumb + Path.DirectorySeparatorChar + "</a>";
                    previous_was_folder = true;
                    
                } else if (System.IO.File.Exists (crumb_path)) {
                    link += "<a href='" + crumb_path + "'>" + crumb + "</a>";
                    previous_was_folder = false;
                    
                } else {
                    if (i > 0 && !previous_was_folder)
                        link += Path.DirectorySeparatorChar;
                    
                    link += crumb;
                    previous_was_folder = false;
                }
                
                new_path_root = SafeCombine (new_path_root, crumb);
                i++;
            }
            
            return link;
        }
        
        
        private string SafeCombine (string path1, string path2)
        {
            string result = path1;
            
            if (!result.EndsWith (Path.DirectorySeparatorChar.ToString ()))
                result += Path.DirectorySeparatorChar;
            
            if (path2.StartsWith (Path.DirectorySeparatorChar.ToString ()))
                path2 = path2.Substring (1);
            
            return result + path2;
        }
        
        
        // All change sets that happened on a day
       /* private class ActivityDay : List<SparkleChangeSet>
        {
            public DateTime Date;
            
            public ActivityDay (DateTime date_time)
            {
                Date = new DateTime (date_time.Year, date_time.Month, date_time.Day);
            }
        }
        
        
        private class RevisionInfo {
            public SparkleFolder Folder;
            public string FilePath;
            public string Revision;
        }
        
        
        private class SparkleDelay : Stopwatch {
            
            public SparkleDelay () : base ()
            {
                Start ();
            }
            
            
            new public void Stop ()
            {
                base.Stop ();
                
                if (ElapsedMilliseconds < 500)
                    Thread.Sleep (500 - (int) ElapsedMilliseconds);
            }
        }*/
    }
}

