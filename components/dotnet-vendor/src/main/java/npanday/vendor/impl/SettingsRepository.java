/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
package npanday.vendor.impl;

import npanday.registry.Repository;
import npanday.registry.RepositoryRegistry;
import org.codehaus.plexus.util.xml.pull.XmlPullParserException;

import java.io.*;
import java.util.Hashtable;
import java.util.List;
import java.util.ArrayList;

import npanday.model.settings.NPandaySettings;
import npanday.model.settings.Vendor;
import npanday.model.settings.DefaultSetup;
import npanday.model.settings.Framework;
import npanday.model.settings.io.xpp3.NPandaySettingsXpp3Reader;
import npanday.PlatformUnsupportedException;
import npanday.vendor.VendorFactory;
import npanday.vendor.VendorInfo;
import npanday.vendor.VendorUnsupportedException;

/**
 *  Provides methods for loading and reading the npanday-settings config file.
 *
 * @author Shane Isbell
 */
public final class SettingsRepository
    implements Repository
{

    /**
     * List of all vendors from the npanday-settings file. The <code>Vendor</code> is the raw model type.
     */
    private List<Vendor> vendors;

    /**
     * The default setup: framework version, vendor, vendor version. If no information is provided by the user, then
     * this information will be used to choose the environment. It may also be used for partial matches, if appropriate.
     */
    private DefaultSetup defaultSetup;

    /**
     * List of all vendors from the npanday-settings file.
     */
    private List<VendorInfo> vendorInfos;

    /**
     * Constructor. This method is intended to be invoked by the <code>RepositoryRegistry<code>, not by the
     * application developer.
     */
    public SettingsRepository()
    {
    }

    /**
     * @see Repository#load(java.io.InputStream, java.util.Hashtable)
     */
    public void load( InputStream inputStream, Hashtable properties )
        throws IOException
    {
        NPandaySettingsXpp3Reader xpp3Reader = new NPandaySettingsXpp3Reader();
        Reader reader = new InputStreamReader( inputStream );
        NPandaySettings settings;
        try
        {
            settings = xpp3Reader.read( reader );
        }
        catch ( XmlPullParserException e )
        {
            e.printStackTrace();
            throw new IOException( "NPANDAY-104-000: Could not read npanday-settings.xml" );
        }
        vendors = settings.getVendors();
        defaultSetup = settings.getDefaultSetup();
        vendorInfos = new ArrayList<VendorInfo>();

        for ( Vendor v : vendors )
        {
            List<Framework> frameworks = v.getFrameworks();
            for ( Framework framework : frameworks )
            {
                VendorInfo vendorInfo = VendorInfo.Factory.createDefaultVendorInfo();
                vendorInfo.setVendorVersion( v.getVendorVersion() );
                List<File> executablePaths = new ArrayList<File>();
                executablePaths.add(new File( framework.getInstallRoot() ));
                if(framework.getSdkInstallRoot() != null)
                {
                    executablePaths.add( new File(framework.getSdkInstallRoot()));
                }
                vendorInfo.setExecutablePaths( executablePaths );
                vendorInfo.setFrameworkVersion( framework.getFrameworkVersion() );
                try
                {
                    vendorInfo.setVendor( VendorFactory.createVendorFromName( v.getVendorName() ) );
                }
                catch ( VendorUnsupportedException e )
                {
                    continue;
                }
                vendorInfo.setDefault(
                    v.getIsDefault() != null && v.getIsDefault().toLowerCase().trim().equals( "true" ) );
                vendorInfos.add( vendorInfo );
            }
        }
    }

    /**
     * @see Repository#setRepositoryRegistry(npanday.registry.RepositoryRegistry)
     */
    public void setRepositoryRegistry( RepositoryRegistry repositoryRegistry )
    {
    }

    /**
     * Returns all vendor infos from the npanday-settings file.
     *
     * @return all vendor infos from the npanday-settings file
     */
    List<VendorInfo> getVendorInfos()
    {
        return vendorInfos;
    }

    File getSdkInstallRootFor( String vendor, String vendorVersion, String frameworkVersion )
        throws PlatformUnsupportedException
    {
        if ( vendor == null || vendorVersion == null || frameworkVersion == null )
        {
            throw new PlatformUnsupportedException( "NPANDAY-104-004: One of more of the parameters is null: Vendor = " +
                vendor + ", Vendor Version = " + vendorVersion + ", Framework Version = " + frameworkVersion );
        }
        for ( Vendor v : vendors )
        {
            if ( vendor.equals( v.getVendorName().trim() ) && vendorVersion.equals( v.getVendorVersion().trim() ) )
            {
                List<Framework> frameworks = v.getFrameworks();
                for ( Framework framework : frameworks )
                {
                    if ( frameworkVersion.equals( framework.getFrameworkVersion().trim() ) )
                    {
                         String sdkRoot = framework.getSdkInstallRoot();                         
                         if(sdkRoot != null) return new File(sdkRoot );
                    }
                }
            }
        }
        return null;
    }

    /**
     *  Returns the install root for the .NET framework for the specified parameters. None of the parameter values
     *  should be null.
     *
     * @param vendor            the vendor name
     * @param vendorVersion     the vendor version
     * @param frameworkVersion  the .NET framework version
     * @return the install root for the .NET framework
     * @throws npanday.PlatformUnsupportedException if there is no install root found for the specified parameters
     */
    public File getInstallRootFor( String vendor, String vendorVersion, String frameworkVersion )
        throws PlatformUnsupportedException
    {
        if ( vendor == null || vendorVersion == null || frameworkVersion == null )
        {
            throw new PlatformUnsupportedException( "NPANDAY-104-001: One of more of the parameters is null: Vendor = " +
                vendor + ", Vendor Version = " + vendorVersion + ", Framework Version = " + frameworkVersion );
        }
        for ( Vendor v : vendors )
        {
        	if ( vendor.equals( v.getVendorName().trim() ) && vendorVersion.equals( v.getVendorVersion().trim() ) )
            {
                List<Framework> frameworks = v.getFrameworks();
              
				for ( Framework framework : frameworks )
                {
					if ( frameworkVersion.equals( framework.getFrameworkVersion().trim() )) 
					{
						return new File( framework.getInstallRoot() );
                    }
                }
            }
        }
        throw new PlatformUnsupportedException( "NPANDAY-104-002: Unable to find install root: Vendor = " + vendor +
            ", Vendor Version = " + vendorVersion + ", Framework Version = " + frameworkVersion );
    }

    /**
     * Returns the default setup: framework version, vendor, vendor version. If no information is provided by the user, then
     * this information will be used to choose the environment. It may also be used for partial matches, if appropriate.
     *
     * @return the default setup: framework version, vendor, vendor version
     */
    DefaultSetup getDefaultSetup()
    {
        return defaultSetup;
    }
}
