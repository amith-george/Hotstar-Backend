pipeline {
 
    agent any
 
    environment {
        IMAGE = "hotstar-api:${BUILD_NUMBER}"
        NETWORK = "hotstar-net"
        MYSQL_CONT = "hotstar-mysql"
        API_CONT = "hotstar-api"
 
        MYSQL_PWD = "appsecret"
        MYSQL_DB = "hotstardb"
    }
 
    stages {
 
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
 
        stage('Build Docker Image') {
            steps {
                bat "docker build -t %IMAGE% ."
            }
        }
 
        stage('Create Network') {
            steps {
                bat "docker network create %NETWORK% 2>nul"
            }
        }
 
        stage('Start MySQL') {
            steps {
                bat """
                docker rm -f %MYSQL_CONT% 2>nul
 
                docker run -d --name %MYSQL_CONT% --network %NETWORK% ^
                    -e MYSQL_ROOT_PASSWORD=%MYSQL_PWD% ^
                    -e MYSQL_DATABASE=%MYSQL_DB% ^
                    -p 3307:3306 ^
                    -v mysql-data:/var/lib/mysql ^
                    mysql:8.0
                """
            }
        }
 
        stage('Wait for MySQL (HEALTHCHECK equivalent)') {
            steps {
                bat """
                echo Waiting for MySQL to be ready...
 
                :loop
                docker exec %MYSQL_CONT% mysqladmin ping -h localhost -uroot -p%MYSQL_PWD% >nul 2>&1
 
                IF ERRORLEVEL 1 (
                    timeout /t 5 >nul
                    goto loop
                )
 
                echo MySQL is ready!
                """
            }
        }
 
        stage('Run API') {
            steps {
                bat """
                docker rm -f %API_CONT% 2>nul
 
                docker run -d --name %API_CONT% --network %NETWORK% ^
                    --env-file .env ^
                    -e ASPNETCORE_ENVIRONMENT=Development ^
                    -e ASPNETCORE_URLS=http://+:5204 ^
                    -e DefaultConnection="Server=%MYSQL_CONT%;Port=3306;Database=%MYSQL_DB%;User=appuser;Password=%MYSQL_PWD%;" ^
                    -p 5204:5204 ^
                    -v api-media:/app/wwwroot/uploads ^
                    %IMAGE%
                """
            }
        }
 
    }
}
 