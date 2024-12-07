pipeline {
    agent any
    environment {
        DOTNET_CLI_HOME = 'C:\\Program Files\\dotnet'
    }

    stages {
        stage('Stop Website') {
            steps {
                script {
                    def siteStatus = bat(script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "YangSpaceApp" /text:state', returnStdout: true).trim()
                    if (siteStatus == 'Stopped') {
                        echo "The site 'YangSpaceApp' is already stopped."
                    } else {
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop site /site.name:"YangSpaceApp"'
                    }

                    def appPoolStatus = bat(script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "YangSpaceApp" /text:state', returnStdout: true).trim()
                    if (appPoolStatus == 'Stopped') {
                        echo "The application pool 'YangSpaceApp' is already stopped."
                    } else {
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop apppool /apppool.name:"YangSpaceApp"'
                    }
                }
            }
        }
        stage('Checkout') {
            steps {
                checkout([$class: 'GitSCM',
                          branches: [[name: '*/main']],
                          doGenerateSubmoduleConfigurations: false,
                          extensions: [],
                          submoduleCfg: [],
                          userRemoteConfigs: [[url: 'https://github.com/jvalkovv/YangSpaceApp.git']]
                ])
            }
        }
        stage('Restore Dependencies') {
            steps {
                script {
                    dir('YangSpaceBackEnd') {
                        bat 'dotnet restore'
                    }
                }
            }
        }
        stage('Build ASP.NET Web API') {
            steps {
                script {
                    dir('YangSpaceBackEnd') {
                        bat 'dotnet build --configuration Release'
                    }
                }
            }
        }
        stage('Build Angular Application') {
            steps {
                script {
                    dir('YangSpaceClient') {
                        bat 'npm install'
                        bat 'npm install typescript@5.5.4 --save-dev'
                        bat 'ng build --configuration production'
                    }
                }
            }
        }
        stage('Publish') {
            steps {
                script {
                    dir('YangSpaceBackEnd') {
                        bat 'dotnet publish --no-restore --configuration Release --output ..\\publish'
                    }
                }
            }
        }
        stage('Copy Files') {
            steps {
                script {
                    bat 'xcopy /s /y publish\\* D:\\Applications\\YangSpaceApp\\YangSpaceBackEnd'
                    bat 'xcopy /s /y YangSpaceClient\\dist\\browser\\* D:\\Applications\\YangSpaceApp\\YangSpaceClient'
                }
            }
        }
        stage('Start Website') {
            steps {
                script {
                    def siteStatus = bat(script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "YangSpaceApp" /text:state', returnStdout: true).trim()
                    if (siteStatus == 'Started') {
                        echo "The site 'YangSpaceApp' is already started."
                    } else {
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd start site /site.name:"YangSpaceApp"'
                    }

                    def appPoolStatus = bat(script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "YangSpaceApp" /text:state', returnStdout: true).trim()
                    if (appPoolStatus == 'Started') {
                        echo "The application pool 'YangSpaceApp' is already started."
                    } else {
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd start apppool /apppool.name:"YangSpaceApp"'
                    }
                }
            }
        }
    }
    post {
        success {
            echo 'Build, publish, and deploy successful!'
        }
        failure {
            echo 'Build, publish, and deploy failed. Check the console output for details.'
        }
    }
}
