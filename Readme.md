# Y messenger server application
This is Y messenger server application. It is developed using .NetCore 2.2 and uses PostgeSQL and Redis as data storage.
## How to start 
1. Install Postgres, Redis.
2. Update appsettings.json 
3. Keep `LicensorURL` empty
## How to start tests?
Tests are located in NodeApp.Tests project. Start them with VisualStudio.
## If I run this app on my local machine, will it connect to other servers?
No. Linux server deployment with the configured domain is required to start cross-node API.
## Would encryption work if I run an application in a Windows-hosted Docker container?
Yes. You should run these commands to do it:
1. Navigate to `NodeApp` directory.
2. run `dotnet publish -r ubuntu.18.04-x64`
3. Navigate to `NodeApp/bin/Debug/netcoreapp2.2/ubuntu.18.04-x64/publish`
4. run `docker-compose build; docker-compose up -d`
The project includes necessary ymessenger_node.dockerfile and docker-compose.yml.
## Did you found a bug? 
Submit an ussie or mail us hi@corp.ymessenger.org
## Do you have new ideas or feature requests? 
Submit an issue or mail us hi@corp.ymessenger.org