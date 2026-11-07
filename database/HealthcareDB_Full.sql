-- ========================================================================
-- SIDSRH — HealthcareDB (Microsoft SQL Server)
-- Run in SSMS or: sqlcmd -S (localdb)\mssqllocaldb -i HealthcareDB_Full.sql
-- ========================================================================
USE master;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'HealthcareDB')
BEGIN
    ALTER DATABASE HealthcareDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE HealthcareDB;
END
GO
CREATE DATABASE HealthcareDB;
GO
USE HealthcareDB;
GO

CREATE TABLE Users (
    UserId NVARCHAR(50) NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
CREATE TABLE Locations (
    LocationId NVARCHAR(50) NOT NULL PRIMARY KEY,
    LocationName NVARCHAR(150) NOT NULL,
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    LocationType NVARCHAR(80) NOT NULL
);
GO
CREATE TABLE MobileClinics (
    ClinicId NVARCHAR(50) NOT NULL PRIMARY KEY,
    ClinicName NVARCHAR(120) NOT NULL,
    LocationId NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Operational',
    ContactNumber NVARCHAR(50) NULL
);
GO
CREATE TABLE Staff (
    StaffId NVARCHAR(50) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    ContactNumber NVARCHAR(50) NULL,
    Role NVARCHAR(80) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
    ClinicId NVARCHAR(50) NULL
);
GO
CREATE TABLE Drones (
    DroneId NVARCHAR(50) NOT NULL PRIMARY KEY,
    DroneName NVARCHAR(100) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Available',
    CurrentLocation NVARCHAR(150) NOT NULL,
    Latitude FLOAT NOT NULL DEFAULT 0,
    Longitude FLOAT NOT NULL DEFAULT 0,
    BatteryLevel INT NOT NULL DEFAULT 100,
    MaxPayloadKg FLOAT NOT NULL DEFAULT 4.5,
    SpeedKmh FLOAT NOT NULL DEFAULT 80,
    HasCamera BIT NOT NULL DEFAULT 1,
    HasVoice BIT NOT NULL DEFAULT 1
);
GO
CREATE TABLE MedicalSupplies (
    SupplyId NVARCHAR(50) NOT NULL PRIMARY KEY,
    SupplyName NVARCHAR(150) NOT NULL,
    SupplyType NVARCHAR(80) NOT NULL,
    Category NVARCHAR(100) NOT NULL DEFAULT '',
    Quantity INT NOT NULL DEFAULT 0,
    ExpiryDate DATETIME2 NOT NULL,
    ColdChain BIT NOT NULL DEFAULT 0
);
GO
CREATE TABLE Deliveries (
    DeliveryId NVARCHAR(50) NOT NULL PRIMARY KEY,
    DroneId NVARCHAR(50) NOT NULL,
    ClinicId NVARCHAR(50) NOT NULL,
    SupplyId NVARCHAR(50) NOT NULL,
    OriginLocationId NVARCHAR(50) NOT NULL,
    DestinationLocationId NVARCHAR(50) NOT NULL,
    DeliveryStatus NVARCHAR(30) NOT NULL DEFAULT 'Pending',
    DistanceKm FLOAT NOT NULL,
    CalculatedCost DECIMAL(18,2) NOT NULL,
    ServiceClass NVARCHAR(20) NOT NULL DEFAULT 'Routine',
    DispatchCode NVARCHAR(20) NULL,
    ArrivalCode NVARCHAR(20) NULL,
    RecipientPhone NVARCHAR(50) NULL,
    RecipientName NVARCHAR(150) NULL,
    DateCreated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    DispatchedAt DATETIME2 NULL,
    EstimatedArrival DATETIME2 NULL,
    DateCompleted DATETIME2 NULL,
    PatientRequestId NVARCHAR(50) NULL
);
GO
CREATE TABLE PatientRequests (
    RequestId NVARCHAR(50) NOT NULL PRIMARY KEY,
    PatientName NVARCHAR(150) NOT NULL,
    ContactNumber NVARCHAR(50) NULL,
    LocationId NVARCHAR(50) NOT NULL,
    MedicalNeed NVARCHAR(255) NOT NULL,
    Category NVARCHAR(100) NOT NULL DEFAULT '',
    Urgency NVARCHAR(20) NOT NULL DEFAULT 'Urgent',
    ServiceClass NVARCHAR(20) NOT NULL DEFAULT 'Urgent',
    Status NVARCHAR(20) NOT NULL DEFAULT 'Submitted',
    DistanceKm FLOAT NULL,
    EstimatedCost DECIMAL(18,2) NULL,
    AssignedDeliveryId NVARCHAR(50) NULL,
    RequestResponder BIT NOT NULL DEFAULT 0,
    HelperName NVARCHAR(150) NULL,
    HelperContact NVARCHAR(50) NULL,
    SituationNotes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
CREATE TABLE Equipment (
    EquipmentId NVARCHAR(50) NOT NULL PRIMARY KEY,
    EquipmentName NVARCHAR(150) NOT NULL,
    EquipmentType NVARCHAR(80) NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Available',
    ClinicId NVARCHAR(50) NULL
);
GO
CREATE TABLE AdministrationGuides (
    GuideId NVARCHAR(50) NOT NULL PRIMARY KEY,
    Category NVARCHAR(100) NOT NULL,
    Situation NVARCHAR(150) NOT NULL,
    Examples NVARCHAR(255) NOT NULL,
    Instructions NVARCHAR(MAX) NOT NULL
);
GO
CREATE TABLE ActivityLogs (
    LogId NVARCHAR(50) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(50) NOT NULL,
    ActivityType NVARCHAR(80) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
CREATE TABLE DispenseRecords (
    DispenseId NVARCHAR(50) NOT NULL PRIMARY KEY,
    ClinicId NVARCHAR(50) NOT NULL,
    SupplyId NVARCHAR(50) NOT NULL,
    PatientName NVARCHAR(150) NOT NULL,
    Quantity INT NOT NULL,
    Notes NVARCHAR(255) NULL,
    DispensedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    StaffUserId NVARCHAR(50) NULL
);
GO

-- NOTE: App seed uses BCrypt hashes. Prefer letting the API EnsureSeed create users
-- when connecting empty HealthcareDB. Optional placeholder rows for structure only:

INSERT INTO Locations VALUES
('LOC001','Mthatha Base Hospital',-31.5889,28.7844,'Hospital'),
('LOC002','Qumbu Mobile Clinic',-31.15,28.8667,'Clinic'),
('LOC003','Tsolo Village Point',-31.30,28.75,'Village'),
('LOC004','Port St Johns Clinic',-31.6289,29.5369,'Clinic'),
('LOC005','Drone Hub – Mthatha',-31.59,28.79,'Base'),
('LOC006','SAPS Mthatha',-31.592,28.788,'Police'),
('LOC007','Home Affairs Mthatha',-31.595,28.782,'Government'),
('LOC008','Qumbu Village Stand',-31.18,28.85,'Village'),
('LOC009','Ngceleni Access Point',-31.22,28.90,'Village');
GO

PRINT 'HealthcareDB created. Start SIDSRH.Api with Provider=SqlServer to seed users via BCrypt.';
GO
