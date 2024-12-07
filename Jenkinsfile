pipeline {
    agent any
    environment {
        DOTNET_CLI_HOME = "C:\\Program Files\\dotnet"
    }

    stages {
        stage('Stop Website') {
            steps {
                script {
                    // Check if the site is already stopped
                    def siteStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "OfficeBiteProd" /text:state',
                        returnStdout: true
                    ).trim()
                    if (siteStatus == 'Stopped') {
                        echo "The site 'OfficeBiteProd' is already stopped."
                    } else {
                        // Stop the specific IIS website
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop site /site.name:"OfficeBiteProd"'
                    }

                    // Check if the application pool is already stopped
                    def appPoolStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "OfficeBiteProd" /text:state',
                        returnStdout: true
                    ).trim()

                    if (appPoolStatus == 'Stopped') {
                        echo "The application pool 'OfficeBiteProd' is already stopped."
                    } else {
                        // Stop the application pool
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop apppool /apppool.name:"OfficeBiteProd"'
                    }
                }
            }
        }
        stage('Checkout') {
            steps {
                // Checkout code from GitHub using the specified SSH credentials
                checkout([$class: 'GitSCM', 
                          branches: [[name: '*/main']], 
                          doGenerateSubmoduleConfigurations: false, 
                          extensions: [], 
                          submoduleCfg: [], 
                          userRemoteConfigs: [[url: 'https://github.com/jvalkovv/YangSpaceApp.git']]
                ])
            }
        }

        stage('Build') {
            steps {
                script {
                    // Restoring dependencies
                    bat "dotnet restore"

                    // Building the application
                    bat "dotnet build --configuration Release"
                }
            }
        }

        stage('Publish') {
            steps {
                script {
                    // Publishing the application
                    bat "dotnet publish --no-restore --configuration Release --output .\\publish"
                }
            }
        }

        stage('Copy Files') {
            steps {
                script {
                    // Perform the copy operation here using xcopy 
                    bat 'xcopy /s /y .\\publish D:\\Applications\\OfficeBiteProd'
                }
            }
        }
        stage('Start Website') {
            steps {
                script {
                    // Check if the site is already started
                    def siteStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "OfficeBiteProd" /text:state',
                        returnStdout: true
                    ).trim()

                    if (siteStatus == 'Started') {
                        echo "The site 'OfficeBiteProd' is already started."
                    } else {
                        // Start the specific IIS website
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd start site /site.name:"OfficeBiteProd"'
                    }

                    // Check if the application pool is already started
                    def appPoolStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "OfficeBiteProd" /text:state',
                        returnStdout: true
                    ).trim()

                    if (appPoolStatus == 'Started') {
                        echo "The application pool 'OfficeBiteProd' is already started."
                    } else {
                        // Start the application pool
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd start apppool /apppool.name:"OfficeBiteProd"'
                    }
                }
            }
        }
    }

    post {
        success {
            echo 'Build, test, publish, and deploy successful!'
        }
    }
}