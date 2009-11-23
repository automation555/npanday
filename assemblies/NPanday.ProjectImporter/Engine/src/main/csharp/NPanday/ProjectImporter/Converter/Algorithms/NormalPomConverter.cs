using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

using NPanday.ProjectImporter.Digest;
using NPanday.ProjectImporter.Digest.Model;
using NPanday.Utils;
using NPanday.Model.Pom;

using NPanday.Artifact;

using System.Reflection;

using NPanday.ProjectImporter.Converter;

using NPanday.ProjectImporter.Validator;


/// Author: Leopoldo Lee Agdeppa III


namespace NPanday.ProjectImporter.Converter.Algorithms
{
    public class NormalPomConverter : AbstractPomConverter
    {

        public NormalPomConverter(ProjectDigest projectDigest, string mainPomFile, NPanday.Model.Pom.Model parent, string groupId) 
            : base(projectDigest,mainPomFile,parent, groupId)
        {
        }



        public override void ConvertProjectToPomModel(bool writePom, string scmTag)
        {

            
            GenerateHeader("library");


            Model.build.sourceDirectory = GetSourceDir();

            //Add SCM Tag
            if (scmTag != null && scmTag != string.Empty && Model.parent==null)
            {
                Scm scmHolder = new Scm();
                scmHolder.connection = string.Format("scm:svn:{0}", scmTag);
                scmHolder.developerConnection = string.Format("scm:svn:{0}", scmTag);
                scmHolder.url = scmTag;

                Model.scm = scmHolder;
            }



            // Add NPanday compile plugin 
            Plugin compilePlugin = AddPlugin(
                "npanday.plugin",
                "maven-compile-plugin",
                null,
                true
            );
            if(!string.IsNullOrEmpty(projectDigest.TargetFramework))
                AddPluginConfiguration(compilePlugin, "frameworkVersion", projectDigest.TargetFramework);
            

            if(projectDigest.Language.Equals("vb",StringComparison.OrdinalIgnoreCase))
            {
                AddPluginConfiguration(compilePlugin, "language", "VB");
                AddPluginConfiguration(compilePlugin, "rootNamespace", projectDigest.RootNamespace);
                string define = GetDefineConfigurationValue();
                if (!string.IsNullOrEmpty(define))
                {
                    AddPluginConfiguration(compilePlugin, "define", define);
                }
            }

            
            AddPluginConfiguration(compilePlugin, "main", projectDigest.StartupObject);
            AddPluginConfiguration(compilePlugin, "doc", projectDigest.DocumentationFile);
            //AddPluginConfiguration(compilePlugin, "noconfig", "true");
            AddPluginConfiguration(compilePlugin, "imports", "import", projectDigest.GlobalNamespaceImports);

            // add include list for the compiling
            DirectoryInfo baseDir = new DirectoryInfo(Path.GetDirectoryName(projectDigest.FullFileName));
            List<string> compiles = new List<string>();
            bool msBuildPluginAdded = false;
            foreach (Compile compile in projectDigest.Compiles)
            {
                string compilesFile = PomHelperUtility.GetRelativePath(baseDir, new FileInfo(compile.IncludeFullPath));
                compiles.Add(compilesFile);

                // if it's a xaml file, include the auto-generated file in object\Debug\
                if (compilesFile.EndsWith(".xaml.cs") || compilesFile.EndsWith(".xaml.vb"))
                { 
                    //add the MsBuild plugin to auto generate the .g.cs/g.vb files
                    if (!msBuildPluginAdded)
                    {
                        Plugin msBuildPlugin = AddPlugin("npanday.plugin", "NPanday.Plugin.Msbuild.JavaBinding", null, true);
                        AddPluginExecution(msBuildPlugin, "compile", "validate");
                        msBuildPluginAdded = true;
                    }
                    

                    string gFile = @"obj\Debug\";
                    if (compilesFile.EndsWith(".cs"))
                        gFile += compilesFile.Replace(".xaml.cs", ".g.cs");
                    else
                        gFile += compilesFile.Replace(".xaml.vb", ".g.vb");

                    string gFullPath = compile.IncludeFullPath.Replace(compilesFile, gFile);
                    
                    compiles.Add(gFile);
                        
                    
                    //Removed because MsBuild plugin assures that the needed files will be present.
                    /*else
                    {
                        // ensure that the auto-generated file is needed by the app to build
                        string xamlFilename = Path.GetFileNameWithoutExtension(compilesFile);
                        if (File.Exists(compile.IncludeFullPath.Replace(Path.GetFileName(compilesFile), xamlFilename)))
                            throw new Exception("Unable to locate XAML auto-generated code. Please run Build in Visual Studio first.");
                    }*/
                }
            }
            AddPluginConfiguration(compilePlugin, "includeSources", "includeSource", compiles.ToArray());
            

            if ("true".Equals(projectDigest.SignAssembly, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(projectDigest.AssemblyOriginatorKeyFile)
                )
            {
                if (Path.IsPathRooted(projectDigest.AssemblyOriginatorKeyFile))
                {
                    AddPluginConfiguration(compilePlugin, "keyfile", PomHelperUtility.GetRelativePath(baseDir, new FileInfo(projectDigest.AssemblyOriginatorKeyFile)));
                }
                else
                {
                    AddPluginConfiguration(compilePlugin, "keyfile", PomHelperUtility.GetRelativePath(baseDir, new FileInfo(baseDir.FullName + @"\" + projectDigest.AssemblyOriginatorKeyFile)));
                }
                
            }
            
            
            // add integration test plugin if project is a test
            if (projectDigest.UnitTest)
            {
                Plugin testPlugin = AddPlugin(
                    "npanday.plugin",
                    "maven-test-plugin",
                    null,
                    true
                );
                AddPluginConfiguration(testPlugin, "integrationTest", "true");
                
            }

            // Add Com Reference Dependencies
            if (projectDigest.ComReferenceList.Length > 0)
            {
                AddComReferenceDependency();
            }
            

			//Add Project WebReferences
            AddWebReferences();

            //Add EmbeddedResources maven-resgen-plugin
            AddEmbeddedResources();
            

            // Add Project Inter-dependencies
            AddInterProjectDependenciesToList();


            // filter the rsp included assemblies
            FilterRSPIncludedReferences();
            // Add Project Reference Dependencies
            AddProjectReferenceDependenciesToList();

            
            if (writePom)
            {
                PomHelperUtility.WriteModelToPom(new FileInfo(Path.Combine(projectDigest.FullDirectoryName, "pom.xml")), Model);
            }

        }



        protected void FilterRSPIncludedReferences()
        {
            List<Reference> list = new List<Reference>();

            foreach (Reference reference in projectDigest.References)
            {
                if (!string.IsNullOrEmpty(projectDigest.Language))
                {
                    if (!gacUtil.IsRspIncluded(reference.Name, projectDigest.Language))
                    {
                        list.Add(reference);
                    }
                }
                else
                {
                    list.Add(reference);
                }
            }
            projectDigest.References = list.ToArray();
        }


    }
}
