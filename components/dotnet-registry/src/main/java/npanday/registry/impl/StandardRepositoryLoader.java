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
package npanday.registry.impl;

import npanday.registry.Repository;
import npanday.registry.RepositoryLoader;
import npanday.registry.RepositoryRegistry;

import java.util.Hashtable;
import java.io.IOException;
import java.io.InputStream;
import java.io.FileInputStream;

/**
 * The default repository loader. This class can be extended
 *
 * @author Shane Isbell
 */

public class StandardRepositoryLoader
    implements RepositoryLoader
{

    private RepositoryRegistry repositoryRegistry;

    /**
     * Takes information from the registry-config file and dynamically builds a <code>Repository</code>
     *
     * @param fileUri         name of the repository's configuration file. It may be located on the file system
     *                        or within a jar.
     * @param repositoryClass name of the repository class
     * @param initParams      <code>Hashtable</code> containing the repository's configuration parameters.
     * @return instance of repository
     * @throws java.io.IOException
     */

    public Repository loadRepository( String fileUri, String repositoryClass, Hashtable initParams )
        throws IOException
    {
        if ( repositoryRegistry == null )
        {
            throw new IOException( "NPANDAY-084-000: The repository registry has not been set." );
        }

        Hashtable props = ( initParams != null ) ? initParams : new Hashtable();

        if ( fileUri == null || fileUri.trim().equals( "" ) )
        {
            throw new IOException( "NPANDAY-084-001: File uri must be provided." );
        }
        if ( repositoryClass == null || repositoryClass.trim().equals( "" ) )
        {
            throw new IOException( "NPANDAY-084-002: Repository class name must be provided: File Name = " + fileUri +
                ", Properties = " + props.toString() );
        }

        InputStream stream;
        Repository repository;
        try
        {
            stream = new FileInputStream( fileUri );
        }
        catch ( IOException e )
        {
            stream = this.getClass().getResourceAsStream( fileUri );
        }
        String message =
            "File Name = " + fileUri + ", Repository Class = " + repositoryClass + ", Properties = " + props.toString();
        boolean optional = ( initParams.containsKey( "optional" ) &&
            ( (String) initParams.get( "optional" ) ).equalsIgnoreCase( "true" ) );
        if ( stream == null && !optional )
        {
            throw new IOException( "NPANDAY-084-003: Unable to loadRegistry config file: " + message );
        }
        else if ( stream == null && optional )
        {
            return null;
        }

        try
        {
            Class c = Class.forName( repositoryClass );
            repository = (Repository) c.newInstance();
            repository.setRepositoryRegistry( repositoryRegistry );
            repository.load( stream, props );

        }
        catch ( IOException e )
        {
            throw new IOException( "NPANDAY-084-004: " + e.toString() + " : " + message );
        }
        catch ( Exception e )
        {
            throw new IOException( "NPANDAY-084-005: " + e.toString() + " : " + message );
        }
        catch ( Error e )
        {
            throw new IOException( "NPANDAY-084-006: " + e.toString() + " : " + message );
        }
        return repository;
    }

    public String getLoaderName()
    {
        return this.getClass().getName();
    }

    public void setRepositoryRegistry( RepositoryRegistry repositoryRegistry )
    {
        this.repositoryRegistry = repositoryRegistry;
    }
}
