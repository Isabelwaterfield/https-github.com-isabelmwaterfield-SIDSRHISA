-- SIDSRH HealthcareDB (Microsoft SQL Server / LocalDB)
-- Use with appsettings Database:Provider = SqlServer
IF DB_ID(N'HealthcareDB') IS NULL CREATE DATABASE HealthcareDB;
GO
USE HealthcareDB;
GO
-- EF Core EnsureCreated will also create tables from the model on first run.
-- This script documents the operational schema for markers and backups.

-- Offline SQLite backup retention (application): ~3 weeks of local files.
-- Online SQL Server: retain operational backups up to 2 years; older sets to archive storage.
