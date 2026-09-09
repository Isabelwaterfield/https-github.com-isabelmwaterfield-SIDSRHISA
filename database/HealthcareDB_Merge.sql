
-- ============================================================
-- HealthcareDB MERGE - keep existing data, add missing pieces
-- Run in SSMS against: (localdb)\MSSQLLocalDB → HealthcareDB
-- ============================================================
USE HealthcareDB;
GO

-- Helper: add column if missing
-- Users
IF COL_LENGTH('dbo.users', 'mfa_enabled') IS NULL
  ALTER TABLE dbo.users ADD mfa_enabled BIT NOT NULL CONSTRAINT DF_users_mfa DEFAULT(0);
IF COL_LENGTH('dbo.users', 'phone') IS NULL
  ALTER TABLE dbo.users ADD phone NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.users', 'created_at') IS NULL
  ALTER TABLE dbo.users ADD created_at DATETIME2 NOT NULL CONSTRAINT DF_users_created DEFAULT SYSUTCDATETIME();
IF COL_LENGTH('dbo.users', 'password_hash') IS NULL AND COL_LENGTH('dbo.users', 'PasswordHash') IS NOT NULL
  PRINT 'Note: check password column name matches password_hash';
GO

-- Drones extra columns
IF COL_LENGTH('dbo.drones', 'latitude') IS NULL ALTER TABLE dbo.drones ADD latitude FLOAT NOT NULL CONSTRAINT DF_dr_lat DEFAULT(0);
IF COL_LENGTH('dbo.drones', 'longitude') IS NULL ALTER TABLE dbo.drones ADD longitude FLOAT NOT NULL CONSTRAINT DF_dr_lng DEFAULT(0);
IF COL_LENGTH('dbo.drones', 'speed_kmh') IS NULL ALTER TABLE dbo.drones ADD speed_kmh FLOAT NOT NULL CONSTRAINT DF_dr_spd DEFAULT(80);
IF COL_LENGTH('dbo.drones', 'max_payload_kg') IS NULL ALTER TABLE dbo.drones ADD max_payload_kg FLOAT NOT NULL CONSTRAINT DF_dr_pay DEFAULT(4.5);
IF COL_LENGTH('dbo.drones', 'has_camera') IS NULL ALTER TABLE dbo.drones ADD has_camera BIT NOT NULL CONSTRAINT DF_dr_cam DEFAULT(1);
IF COL_LENGTH('dbo.drones', 'has_voice') IS NULL ALTER TABLE dbo.drones ADD has_voice BIT NOT NULL CONSTRAINT DF_dr_voi DEFAULT(1);
IF COL_LENGTH('dbo.drones', 'current_location') IS NULL ALTER TABLE dbo.drones ADD current_location NVARCHAR(150) NOT NULL CONSTRAINT DF_dr_loc DEFAULT('');
GO

-- Deliveries extended
IF COL_LENGTH('dbo.deliveries', 'service_class') IS NULL ALTER TABLE dbo.deliveries ADD service_class NVARCHAR(20) NOT NULL CONSTRAINT DF_del_sc DEFAULT('Routine');
IF COL_LENGTH('dbo.deliveries', 'dispatch_code') IS NULL ALTER TABLE dbo.deliveries ADD dispatch_code NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.deliveries', 'arrival_code') IS NULL ALTER TABLE dbo.deliveries ADD arrival_code NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.deliveries', 'recipient_phone') IS NULL ALTER TABLE dbo.deliveries ADD recipient_phone NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.deliveries', 'recipient_name') IS NULL ALTER TABLE dbo.deliveries ADD recipient_name NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.deliveries', 'dispatched_at') IS NULL ALTER TABLE dbo.deliveries ADD dispatched_at DATETIME2 NULL;
IF COL_LENGTH('dbo.deliveries', 'estimated_arrival') IS NULL ALTER TABLE dbo.deliveries ADD estimated_arrival DATETIME2 NULL;
IF COL_LENGTH('dbo.deliveries', 'patient_request_id') IS NULL ALTER TABLE dbo.deliveries ADD patient_request_id NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.deliveries', 'patient_fee') IS NULL ALTER TABLE dbo.deliveries ADD patient_fee DECIMAL(18,2) NOT NULL CONSTRAINT DF_del_pf DEFAULT(0);
IF COL_LENGTH('dbo.deliveries', 'actual_cost') IS NULL ALTER TABLE dbo.deliveries ADD actual_cost DECIMAL(18,2) NOT NULL CONSTRAINT DF_del_ac DEFAULT(0);
IF COL_LENGTH('dbo.deliveries', 'weather_hold_station_id') IS NULL ALTER TABLE dbo.deliveries ADD weather_hold_station_id NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.deliveries', 'delivery_note') IS NULL ALTER TABLE dbo.deliveries ADD delivery_note NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.deliveries', 'collection_deadline') IS NULL ALTER TABLE dbo.deliveries ADD collection_deadline DATETIME2 NULL;
IF COL_LENGTH('dbo.deliveries', 'delay_fine') IS NULL ALTER TABLE dbo.deliveries ADD delay_fine DECIMAL(18,2) NOT NULL CONSTRAINT DF_del_df DEFAULT(0);
IF COL_LENGTH('dbo.deliveries', 'brief_reason') IS NULL ALTER TABLE dbo.deliveries ADD brief_reason NVARCHAR(80) NULL;
IF COL_LENGTH('dbo.deliveries', 'technician_name') IS NULL ALTER TABLE dbo.deliveries ADD technician_name NVARCHAR(150) NULL;
GO

-- Patient requests extended
IF COL_LENGTH('dbo.patient_requests', 'category') IS NULL ALTER TABLE dbo.patient_requests ADD category NVARCHAR(100) NOT NULL CONSTRAINT DF_pr_cat DEFAULT('');
IF COL_LENGTH('dbo.patient_requests', 'service_class') IS NULL ALTER TABLE dbo.patient_requests ADD service_class NVARCHAR(20) NOT NULL CONSTRAINT DF_pr_sc DEFAULT('Urgent');
IF COL_LENGTH('dbo.patient_requests', 'request_responder') IS NULL ALTER TABLE dbo.patient_requests ADD request_responder BIT NOT NULL CONSTRAINT DF_pr_rr DEFAULT(0);
IF COL_LENGTH('dbo.patient_requests', 'helper_name') IS NULL ALTER TABLE dbo.patient_requests ADD helper_name NVARCHAR(150) NULL;
IF COL_LENGTH('dbo.patient_requests', 'helper_contact') IS NULL ALTER TABLE dbo.patient_requests ADD helper_contact NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.patient_requests', 'situation_notes') IS NULL ALTER TABLE dbo.patient_requests ADD situation_notes NVARCHAR(MAX) NULL;
IF COL_LENGTH('dbo.patient_requests', 'created_at') IS NULL ALTER TABLE dbo.patient_requests ADD created_at DATETIME2 NOT NULL CONSTRAINT DF_pr_ca DEFAULT SYSUTCDATETIME();
GO

-- Medical supplies category
IF COL_LENGTH('dbo.medical_supplies', 'category') IS NULL ALTER TABLE dbo.medical_supplies ADD category NVARCHAR(100) NOT NULL CONSTRAINT DF_ms_cat DEFAULT('');
GO

-- NEW TABLES
IF OBJECT_ID('dbo.administration_guides','U') IS NULL
CREATE TABLE dbo.administration_guides (
  guide_id NVARCHAR(50) NOT NULL PRIMARY KEY,
  category NVARCHAR(100) NOT NULL,
  situation NVARCHAR(150) NOT NULL,
  examples NVARCHAR(255) NOT NULL,
  instructions NVARCHAR(MAX) NOT NULL
);
GO

IF OBJECT_ID('dbo.dispense_records','U') IS NULL
CREATE TABLE dbo.dispense_records (
  dispense_id NVARCHAR(50) NOT NULL PRIMARY KEY,
  clinic_id NVARCHAR(50) NOT NULL,
  supply_id NVARCHAR(50) NOT NULL,
  patient_name NVARCHAR(150) NOT NULL,
  patient_id NVARCHAR(50) NULL,
  target_type NVARCHAR(30) NOT NULL DEFAULT 'Patient',
  to_clinic_id NVARCHAR(50) NULL,
  quantity INT NOT NULL,
  notes NVARCHAR(255) NULL,
  dispensed_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  staff_user_id NVARCHAR(50) NULL,
  status NVARCHAR(30) NOT NULL DEFAULT 'Completed',
  verified_by_user_id NVARCHAR(50) NULL
);
GO

IF OBJECT_ID('dbo.patient_profiles','U') IS NULL
CREATE TABLE dbo.patient_profiles (
  patient_id NVARCHAR(50) NOT NULL PRIMARY KEY,
  full_name NVARCHAR(150) NOT NULL,
  contact_number NVARCHAR(50) NOT NULL DEFAULT '',
  id_number NVARCHAR(50) NULL,
  location_id NVARCHAR(50) NULL,
  notes NVARCHAR(255) NULL,
  status NVARCHAR(20) NOT NULL DEFAULT 'Active',
  created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  created_by_user_id NVARCHAR(50) NULL
);
GO

IF OBJECT_ID('dbo.chronic_schedules','U') IS NULL
CREATE TABLE dbo.chronic_schedules (
  schedule_id NVARCHAR(50) NOT NULL PRIMARY KEY,
  patient_id NVARCHAR(50) NOT NULL,
  supply_id NVARCHAR(50) NOT NULL,
  clinic_id NVARCHAR(50) NOT NULL,
  interval_days INT NOT NULL DEFAULT 30,
  next_due_date DATETIME2 NOT NULL,
  preferred_slot NVARCHAR(10) NOT NULL DEFAULT '09:00',
  status NVARCHAR(20) NOT NULL DEFAULT 'Active',
  notes NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.charging_stations','U') IS NULL
CREATE TABLE dbo.charging_stations (
  station_id NVARCHAR(50) NOT NULL PRIMARY KEY,
  station_name NVARCHAR(150) NOT NULL,
  latitude FLOAT NOT NULL,
  longitude FLOAT NOT NULL,
  station_type NVARCHAR(30) NOT NULL DEFAULT 'Tower',
  coverage_radius_km INT NOT NULL DEFAULT 15,
  status NVARCHAR(30) NOT NULL DEFAULT 'Operational'
);
GO

IF OBJECT_ID('dbo.system_weather','U') IS NULL
CREATE TABLE dbo.system_weather (
  id INT NOT NULL PRIMARY KEY,
  condition NVARCHAR(20) NOT NULL DEFAULT 'Clear',
  updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
  note NVARCHAR(255) NULL
);
GO

IF OBJECT_ID('dbo.otp_challenges','U') IS NULL
CREATE TABLE dbo.otp_challenges (
  otp_id NVARCHAR(50) NOT NULL PRIMARY KEY,
  email NVARCHAR(150) NOT NULL,
  code_hash NVARCHAR(255) NOT NULL,
  purpose NVARCHAR(30) NOT NULL,
  expires_at DATETIME2 NOT NULL,
  used BIT NOT NULL DEFAULT 0,
  created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

IF OBJECT_ID('dbo.clinic_appointments','U') IS NULL
CREATE TABLE dbo.clinic_appointments (
  appointment_id NVARCHAR(50) NOT NULL PRIMARY KEY,
  patient_id NVARCHAR(50) NOT NULL,
  patient_name NVARCHAR(150) NOT NULL,
  appointment_at DATETIME2 NOT NULL,
  clinic_id NVARCHAR(50) NOT NULL,
  reason_brief NVARCHAR(80) NOT NULL DEFAULT 'Follow-up checkup',
  status NVARCHAR(20) NOT NULL DEFAULT 'Scheduled'
);
GO

-- Seed weather row
IF NOT EXISTS (SELECT 1 FROM dbo.system_weather WHERE id = 1)
  INSERT INTO dbo.system_weather (id, condition, note) VALUES (1, 'Clear', 'Default');
GO

-- Charging stations sample if empty
IF NOT EXISTS (SELECT 1 FROM dbo.charging_stations)
BEGIN
  INSERT INTO dbo.charging_stations VALUES
  ('CHG001','Mthatha Tower Hub',-31.59,28.79,'Tower',15,'Operational'),
  ('CHG002','Qumbu Cell Tower Pad',-31.15,28.8667,'Tower',15,'Operational'),
  ('CHG003','Tsolo Underground Cache',-31.30,28.75,'Underground',15,'Operational'),
  ('CHG004','Port St Johns Tower',-31.6289,29.5369,'Tower',15,'Operational');
END
GO

PRINT 'HealthcareDB merge complete. Keep emergency_scenarios/simulations tables as-is (unused by app).';
GO
