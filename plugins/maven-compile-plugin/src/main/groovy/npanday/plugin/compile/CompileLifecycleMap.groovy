/* 
 * Copyright 2010 NPanday
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package npanday.plugin.compile;

import org.apache.maven.lifecycle.Lifecycle;
import npanday.lifecycle.LifecycleMapping;
import npanday.lifecycle.LifecyclePhase;
import npanday.lifecycle.LifecycleMap;
import npanday.ArtifactType;

/**
 * The lifecycles defined by the maven-compile-plugin..
 * 
 * @author <a href="mailto:me@lcorneliussen.de">Lars Corneliussen</a>
 */
class CompileLifecycleMap extends LifecycleMap
{
	
	void defineMappings() {
		add(new LifecycleMapping(type: ArtifactType.LIBRARY, phases: null))
	}
}
