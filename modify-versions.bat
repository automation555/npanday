cd imports/maven-pom-plugin
call mvn install
cd ../..
call mvn install -f pom-modify-versions.xml -Dmodify-version -DnpandayVersion=%1 -DmavenVersion=%2