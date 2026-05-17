-- Guide language (required for GUIDE type) and optional contact email for notifications
ALTER TABLE tbl_staff
  ADD COLUMN IF NOT EXISTS language VARCHAR(100),
  ADD COLUMN IF NOT EXISTS email VARCHAR(255);
