//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using System;
using System.IO;
using System.Collections;
using System.Xml.Serialization;
using Microsoft.Win32;

using NPanday.Plugin;

namespace NPanday.Plugin.Settings
{
	[Serializable]
    [ClassAttribute(Phase = "validate", Goal = "generate-settings")]
	public sealed class SettingsGeneratorMojo : AbstractMojo
    {
        public SettingsGeneratorMojo() { }

		public override Type GetMojoImplementationType()
		{
			return this.GetType();
		}

        public override void Execute()
        {
			Console.WriteLine("Hello World!");
			
        	string outputFile = Environment.GetEnvironmentVariable("USERPROFILE") + "/.m2/npanday-settings.xml";
			
			Console.WriteLine(outputFile);
            XmlSerializer serializer = new XmlSerializer(typeof(npandaySettings));

            npandaySettings  settings = new npandaySettings();
            settings.operatingSystem = Environment.OSVersion.ToString();

            RegistryKey monoRegistryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Novell\Mono");
            RegistryKey microsoftRegistryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");

            string defaultMonoCLR = (monoRegistryKey != null) ? (string) monoRegistryKey.GetValue("DefaultCLR") : null;

            settings.defaultSetup = GetDefaultSetup(defaultMonoCLR,
                (string) microsoftRegistryKey.GetValue("InstallRoot"));

            npandaySettingsVendor[] microsoftVendors = null;
            try
            {
                microsoftVendors = GetVendorsForMicrosoft(microsoftRegistryKey);
            }
            catch(ExecutionException e)
            {
                Console.WriteLine(e.ToString());
            }

            npandaySettingsVendor[] monoVendors = null;
            try
            {
                monoVendors = GetVendorsForMono(monoRegistryKey, defaultMonoCLR);
            }
            catch(ExecutionException e)
            {
                Console.WriteLine(e.ToString());
            }

            npandaySettingsVendor dotGnuVendor = null;
            try
            {
                dotGnuVendor = GetVendorForGnu(Environment.GetEnvironmentVariable("CSCC_LIB_PATH"));
            }
            catch(ExecutionException e)
            {
                Console.WriteLine(e.ToString());
            }
            int monoVendorsLength = (monoVendors == null) ? 0: monoVendors.Length;
            int dotGnuVendorLength = (dotGnuVendor == null) ? 0: 1;
            int microsoftVendorsLength = (microsoftVendors == null) ? 0: microsoftVendors.Length;

            npandaySettingsVendor[] vendors =
                new npandaySettingsVendor[microsoftVendorsLength + monoVendorsLength + dotGnuVendorLength];

            int copyLocation = 0;
            if(microsoftVendors != null)
            {
                microsoftVendors.CopyTo(vendors, copyLocation);
                copyLocation+=microsoftVendors.Length;
            }
            if(monoVendors != null)
            {
                monoVendors.CopyTo(vendors, copyLocation);
                copyLocation+=monoVendors.Length;
            }
            if(dotGnuVendor != null)
                vendors[copyLocation] = dotGnuVendor;

            settings.vendors = vendors;
            TextWriter writer = new StreamWriter(@outputFile);
            serializer.Serialize(writer, settings);
            writer.Close();
        }

        protected npandaySettingsDefaultSetup GetDefaultSetup(string defaultMonoCLR,
                                                             string installRoot)
        {
            npandaySettingsDefaultSetup defaultSetup = new npandaySettingsDefaultSetup();
            if(installRoot == null)
            {
                defaultSetup.vendorName = "MONO";
                defaultSetup.vendorVersion = defaultMonoCLR;
                return (defaultMonoCLR != null) ? defaultSetup : null;
            }
            bool dirInfo11 = new DirectoryInfo(Path.Combine(installRoot, "v1.1.4322")).Exists;
            bool dirInfo20 = new DirectoryInfo(Path.Combine(installRoot, "v2.0.50727")).Exists;
            bool dirInfo35 = new DirectoryInfo(Path.Combine(installRoot, "v3.5")).Exists;

            if(installRoot != null)
            {
                if(!dirInfo11 && !dirInfo20 && !dirInfo35)
                    return null;
                defaultSetup.vendorName = "MICROSOFT";
                defaultSetup.vendorVersion = (dirInfo20) ? "2.0.50727" : ((dirInfo35) ? "3.5" : "1.1.4322");
                defaultSetup.frameworkVersion = defaultSetup.vendorVersion;
                return defaultSetup;
            }
            else if(defaultMonoCLR != null)
            {
                defaultSetup.vendorName = "MONO";
                defaultSetup.vendorVersion = defaultMonoCLR;
                defaultSetup.frameworkVersion = "2.0.50727";
                return defaultSetup;
            }
            return null;
        }

        protected npandaySettingsVendor GetVendorForGnu(String libPath)
        {
            if(libPath == null)
                throw new ExecutionException("NPANDAY-9011-000: No CSCC_LIB_PATH Found");

            if (libPath.EndsWith("lib" + Path.DirectorySeparatorChar + "cscc" + Path.DirectorySeparatorChar + "lib"))
            {
                string installR = new DirectoryInfo(libPath).Parent.Parent.Parent.FullName;
                string[] tokenizedInstallRoot = installR.Split(Path.DirectorySeparatorChar);
                string vendorVersion = tokenizedInstallRoot[tokenizedInstallRoot.Length - 1];
                if (!isValidVersion(vendorVersion))
                {
                    throw new ExecutionException("NPANDAY-9011-001: Invalid version format for dotGNU: Version = " +
                        vendorVersion + ", Root = " + installR);
                }

                npandaySettingsVendor vendor = new npandaySettingsVendor();
                vendor.vendorName = "DotGNU";
                vendor.vendorVersion = vendorVersion;
                npandaySettingsVendorFramework[] vendorFrameworks 
                	= new npandaySettingsVendorFramework[1];
                npandaySettingsVendorFramework vf = new npandaySettingsVendorFramework();
                vf.installRoot = Path.Combine(installR, "bin");
                vf.frameworkVersion = "2.0.50727";//doesn't matter
                vendorFrameworks[0] = vf;                                                    ;
                vendor.frameworks = vendorFrameworks;
                return vendor;
            }
            throw new ExecutionException("NPANDAY-9011-002: CSCC_LIB_PATH found but could not determine vendor information");
        }

        private npandaySettingsVendor[] GetVendorsForMicrosoft(RegistryKey microsoftRegistryKey)
        {
            if(microsoftRegistryKey == null)
                throw new ExecutionException("NPANDAY-9011-006: Microsoft installation could not be found.");
            string installRoot = (string) microsoftRegistryKey.GetValue("InstallRoot");
            string sdkInstallRoot11 = (string) microsoftRegistryKey.GetValue("sdkInstallRootv1.1");
            string sdkInstallRoot20 = (string) microsoftRegistryKey.GetValue("sdkInstallRootv2.0");
            string sdkInstallRoot35 = (string) microsoftRegistryKey.GetValue("sdkInstallRootv3.5");
            
            if(installRoot == null) throw new ExecutionException("NPANDAY-9011-005");

            npandaySettingsVendor[] vendors = new npandaySettingsVendor[4];
            DirectoryInfo dirInfo11 = new DirectoryInfo(Path.Combine(installRoot, "v1.1.4322"));
            DirectoryInfo dirInfo20 = new DirectoryInfo(Path.Combine(installRoot, "v2.0.50727"));
            DirectoryInfo dirInfo30 = new DirectoryInfo(Path.Combine(installRoot, "v3.0"));
            DirectoryInfo dirInfo35 = new DirectoryInfo(Path.Combine(installRoot, "v3.5"));
            int vendorCounter = 0;
            if (dirInfo11.Exists)
            {
                npandaySettingsVendor vendor = new npandaySettingsVendor();
                vendor.vendorName = "MICROSOFT";
                vendor.vendorVersion = "1.1.4322";
                npandaySettingsVendorFramework[] vendorFrameworks 
                	= new npandaySettingsVendorFramework[1];
                npandaySettingsVendorFramework vf11 
                	= new npandaySettingsVendorFramework();
                vf11.installRoot = dirInfo11.FullName;
                vf11.frameworkVersion = "1.1.4322";
                
                vendorFrameworks[0] = vf11;
                vf11.sdkInstallRoot = sdkInstallRoot11;
                vendor.frameworks = vendorFrameworks;
                
                vendors[vendorCounter++] = vendor;
            }
            if (dirInfo20.Exists)
            {
                npandaySettingsVendor vendor = new npandaySettingsVendor();
                vendor.vendorName = "MICROSOFT";
                vendor.vendorVersion = "2.0.50727";
                npandaySettingsVendorFramework[] vendorFrameworks 
                	= new npandaySettingsVendorFramework[1];
                npandaySettingsVendorFramework vf11 = new npandaySettingsVendorFramework();
                vf11.installRoot = dirInfo20.FullName;
                vf11.frameworkVersion = "2.0.50727";
                vendorFrameworks[0] = vf11;
                vf11.sdkInstallRoot = sdkInstallRoot20;
                vendor.frameworks = vendorFrameworks;
                vendors[vendorCounter++] = vendor;
            }
            if (dirInfo30.Exists)
            {
                npandaySettingsVendor vendor = new npandaySettingsVendor();
                vendor.vendorName = "MICROSOFT";
                vendor.vendorVersion = "3.0";
                npandaySettingsVendorFramework[] vendorFrameworks = new npandaySettingsVendorFramework[1];
                npandaySettingsVendorFramework vf11 = new npandaySettingsVendorFramework();
                vf11.installRoot = dirInfo30.FullName;
                vf11.frameworkVersion = "3.0";
                vendorFrameworks[0] = vf11;
                vf11.sdkInstallRoot = sdkInstallRoot20;
                vendor.frameworks = vendorFrameworks;
                vendors[vendorCounter++] = vendor;
            }
            if (dirInfo35.Exists)
            {
                npandaySettingsVendor vendor = new npandaySettingsVendor();
                vendor.vendorName = "MICROSOFT";
                vendor.vendorVersion = "3.5";
                npandaySettingsVendorFramework[] vendorFrameworks = new npandaySettingsVendorFramework[1];
                npandaySettingsVendorFramework vf11 = new npandaySettingsVendorFramework();
                vf11.installRoot = dirInfo35.FullName;
                vf11.frameworkVersion = "3.5";
                vendorFrameworks[0] = vf11;
                vf11.sdkInstallRoot = sdkInstallRoot20;
                vendor.frameworks = vendorFrameworks;
                vendors[vendorCounter++] = vendor;
            }

            return vendors;
        }

        private npandaySettingsVendor[] GetVendorsForMono(RegistryKey monoRegistryKey, string defaultMonoCLR)
        {
            if(monoRegistryKey == null)
                throw new ExecutionException("NPANDAY-9011-007: Mono installation czould not be found.");
            npandaySettingsVendor[] vendors = new npandaySettingsVendor[monoRegistryKey.SubKeyCount];
            int i = 0;
            foreach (string keyName in monoRegistryKey.GetSubKeyNames())
            {
                string sdkInstallRoot = (string) monoRegistryKey.OpenSubKey(keyName).GetValue("SdkInstallRoot");
                if(sdkInstallRoot == null)
                    throw new ExecutionException("NPANDAY-9011-004: Could not find install root key for mono");
                string installRoot = Path.Combine(sdkInstallRoot, "bin");
                npandaySettingsVendorFramework[] vendorFrameworks = new npandaySettingsVendorFramework[2];
                npandaySettingsVendorFramework vf11 = new npandaySettingsVendorFramework();
                vf11.installRoot = installRoot;
                vf11.frameworkVersion = "1.1.4322";
                vendorFrameworks[0] = vf11;

                npandaySettingsVendorFramework vf20 = new npandaySettingsVendorFramework();
                vf20.installRoot = installRoot;
                vf20.frameworkVersion = "2.0.50727";
                vendorFrameworks[1] = vf20;

                npandaySettingsVendorFramework vf35 = new npandaySettingsVendorFramework();
                vf35.installRoot = installRoot;
                vf35.frameworkVersion = "3.5";
                vendorFrameworks[2] = vf35;

                npandaySettingsVendor vendor = new npandaySettingsVendor();
                vendor.vendorName = "MONO";
                vendor.vendorVersion = keyName;
                vendor.frameworks = vendorFrameworks;
                if(defaultMonoCLR.Equals(keyName)) vendor.isDefault = "true";
                vendors[i++] = vendor;
            }
            return vendors;
        }

        private bool isValidVersion(String version)
        {
            string[] vendorVersionToken = version.Split('.');
            foreach (string token in vendorVersionToken)
            {
            	try
            	{
            		Single.Parse(token);
            	}
            	catch(Exception)
            	{
            		return false;
            	}
            }
            return true;
        }
    }
}
