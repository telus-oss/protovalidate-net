properties([
    parameters([
        booleanParam(name: 'forcePublishNuget', defaultValue: false, description: 'Publish Nuget (Force)')
	])		  
])
	
def forcePublishNuget = params.forcePublishNuget

node("windows") {   
    stage('Checkout') {
        echo 'Checking Out Source ....'
        checkout scm
        echo 'Checked Out Source ....'
    }
    
    stage('Build') {
        echo 'Building and publishing....'
        
        withCredentials([usernameColonPassword(credentialsId: 'jenkins_artifactory', variable: 'artifactoryNugetApiKey')]) {
            def gradleCommand = "gradlew.bat publish \"-PartifactoryNugetApiKey=$artifactoryNugetApiKey\"" 
                         
            if (forcePublishNuget) {
                gradleCommand = gradleCommand + " -PdoNugetPublish"
            }
            
            bat gradleCommand             
        }       
        
        def buildProps = readJSON file: "${env.WORKSPACE}/build/version.json"
        
        def isProductionVersion = buildProps.isProductionVersion
        def nugetVersion = buildProps.nugetVersion

        if (isProductionVersion || forcePublishNuget) {
            currentBuild.description = nugetVersion
        }   
    }
}