ASSEMBLY = SparkleLib.SQ
TARGET = library

ASSEMBLY_INFO_SOURCE = ../Defines.cs

LINK = -r:$(DIR_BIN)/SparkleLib.dll

SOURCES =  \
	Authentication.cs \
	Change.cs \
	ConnectionManager.cs \
	Constant.cs \
	Credential.cs \
	File.cs \
	FileWatcher.cs \
	Folder.cs \
	LocalFile.cs \
	LocalRepo.cs \
	Logger.cs \
	OSXFileWatcher.cs \
	QloudSyncPlugin.cs \
	RemoteFile.cs \
	RemoteRepo \
	Repo \
	Sync \
	SparkleFetcherSQ.cs \
	SparkleRepoSQ.cs \
	Synchronizer.cs \
	SynchronizerDownload.cs \
	SynchronizerUpload.cs \
	AssemblyInfo.cs


install-data-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(EXTRA_BUNDLE); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

include $(top_srcdir)/build/build.mk
