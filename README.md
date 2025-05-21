Here's my app settings configuration:
(Please ignore what I put here before :)

In my configuration file there is a "ConnectionString" which holds the value for the "UniversityConnection"

In that connection I have  -> 

Server for server address,

Database for database name (s*****)

TrustedConnection and TrustServerCertificate can be true or false. In my case both are true.
***********

I chose not to split my app into several projects. I did not want to add extra complexity or make the task harder.
Since EF generates most of the things (even tables and relationships). So I directly focused on creating my endpoints.
I already can reach everything via my dbContext using LINQ wihout needing of an service/repository class. 
Also our homework was a small project so I think, it is unnecessary to creating several projects for that task.
Instead of that I created 3 directories(DAL, DTOs, Models) for better visualization of my structure and kept clean.
