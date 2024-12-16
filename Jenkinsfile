pipeline {
    agent any
    environment {
        DOTNET_CLI_HOME = "C:\\Program Files\\dotnet"
    }

    stages {
        stage('Stop Website') {
            steps {
                script {
                    def siteStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()
                    if (siteStatus != 'Stopped') {
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop site /site.name:"YangSpaceApp"'
                    }

                    def appPoolStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()
                    if (appPoolStatus != 'Stopped') {
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd stop apppool /apppool.name:"YangSpaceApp"'
                    }
                }
            }
        }

        stage('Checkout') {
            steps {
                checkout([$class: 'GitSCM', 
                          branches: [[name: '*/main']], 
                          userRemoteConfigs: [[url: 'https://github.com/jvalkovv/YangSpaceApp.git']]
                ])
            }
        }

        stage('Build .NET Backend') {
            steps {
                script {
                    bat "dotnet restore"
                    bat "dotnet build --configuration Release"
                }
            }
        }

        stage('Test .NET Backend') {
            steps {
                script {
                    bat "dotnet test --no-restore --configuration Release"
                }
            }
        }

        stage('Publish .NET Backend') {
            steps {
                script {
                    bat "dotnet publish --no-restore --configuration Release --output .\\publish"
                }
            }
        }

        stage('Build Angular Frontend') {
            steps {
                script {
                    dir('yangspaceapp.client') { 
                        echo 'Installing Angular dependencies...'
                        bat 'npm install'

                        echo 'Building Angular application...'
                        bat 'npm run build -- --configuration=production'
                    }
                }
            }
        }

        stage('Copy Files') {
            steps {
                script {
                    echo 'Copying .NET backend files...'
                    bat 'xcopy /s /y .\\publish D:\\Applications\\YangSpaceApp'

                    echo 'Copying Angular frontend files to wwwroot...'
                    bat 'xcopy /s /y yangspaceapp.client\\dist\\browser\* D:\\Applications\\YangSpaceApp\\wwwroot'
                }
            }
        }

        stage('Start Website') {
            steps {
                script {
                    def siteStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list site "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()
                    if (siteStatus != 'Started') {
                        bat 'C:\\Windows\\System32\\inetsrv\\appcmd start site /site.name:"YangSpaceApp"'
                    }

                    def appPoolStatus = bat(
                        script: 'C:\\Windows\\System32\\inetsrv\\appcmd list apppool "YangSpaceApp" /text:state',
                        returnStdout: true
                    ).trim()
                    if (appPoolStatus != 'Started') {
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
        failure {
            echo 'Deployment failed!'
        }
    }
}
