pipeline {
    agent any
    environment {
        DOTNET_CLI_HOME = 'C:\\Program Files\\dotnet'
    }

    stages {
        stage('Stop Website') {
            steps {
                script {
                    // Check if the site is already stopped
                    def siteStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()
                    if (siteStatus == 'Stopped') {
                        echo "The site 'YangSpaceApp' is already stopped."
                    } else {
                        // Stop the specific IIS website
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop site /site.name:"YangSpaceApp"'
                    }

                    // Check if the application pool is already stopped
                    def appPoolStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()

                    if (appPoolStatus == 'Stopped') {
                        echo "The application pool 'YangSpaceApp' is already stopped."
                    } else {
                        // Stop the application pool
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop apppool /apppool.name:"YangSpaceApp"'
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

        stage('Build ASP.NET Web API') {
            steps {
                script {
                    dir('YangSpaceBackEnd') {
                        // Restoring dependencies
                        bat 'dotnet restore'

                        // Building the application
                        bat 'dotnet build --configuration Release'
                    }
                }
            }
        }
        stage('Build Angular Application') {
            steps {
                script {
                    dir('YangSpaceClient') {
                        // Install dependencies
                        bat 'npm install'
                        // Build the Angular application
                        bat 'ng build --prod'
                    }
                }
            }
        }

        stage('Publish') {
            steps {
                script {
                    // Publishing the application
                    bat 'dotnet publish --no-restore --configuration Release --output .\\publish'
                }
            }
        }
        stage('Copy Files') {
            steps {
                script {
                    // Copy ASP.NET Web API files
                    bat 'xcopy /s /y .\\publish D:\\Applications\\YangSpaceApp\\YangSpaceBackEnd'
                    // Copy Angular build files
                    bat 'xcopy /s /y AngularApp\\dist\\* D:\\Applications\\YangSpaceApp\\app\\YangSpaceClient'
                }
            }
        }

        stage('Start Website') {
            steps {
                script {
                    // Check if the site is already started
                    def siteStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()

                    if (siteStatus == 'Started') {
                        echo "The site 'YangSpaceApp' is already started."
                    } else {
                        // Start the specific IIS website
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd start site /site.name:"YangSpaceApp"'
                    }

                    // Check if the application pool is already started
                    def appPoolStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()

                    if (appPoolStatus == 'Started') {
                        echo "The application pool 'YangSpaceApp' is already started."
                    } else {
                        // Start the application pool
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd start apppool /apppool.name:"YangSpaceApp"'
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
