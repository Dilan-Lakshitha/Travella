-- Adds required driver/guide fields to tbl_staff.
-- Run this once in your PostgreSQL database.

ALTER TABLE tbl_staff
  ADD COLUMN IF NOT EXISTS phone VARCHAR(50),
  ADD COLUMN IF NOT EXISTS experience VARCHAR(100),
  ADD COLUMN IF NOT EXISTS availability VARCHAR(20) DEFAULT 'AVAILABLE';

-- Optional: normalize existing rows
UPDATE tbl_staff
SET availability = COALESCE(NULLIF(availability, ''), 'AVAILABLE')
WHERE availability IS NULL OR availability = '';

