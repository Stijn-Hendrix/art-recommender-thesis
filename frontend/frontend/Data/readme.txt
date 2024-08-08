Add-Migration "message"
Update-Database

DROP SCHEMA public CASCADE;
CREATE SCHEMA public;