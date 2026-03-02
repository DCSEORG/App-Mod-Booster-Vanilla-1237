-- script.sql
-- Creates the managed identity user in the database with appropriate roles
-- The placeholder MANAGED-IDENTITY-NAME is replaced by deploy.sh using sed

IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'MANAGED-IDENTITY-NAME')
BEGIN
    DROP USER [MANAGED-IDENTITY-NAME];
END

CREATE USER [MANAGED-IDENTITY-NAME] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [MANAGED-IDENTITY-NAME];
ALTER ROLE db_datawriter ADD MEMBER [MANAGED-IDENTITY-NAME];
GRANT EXECUTE TO [MANAGED-IDENTITY-NAME];
