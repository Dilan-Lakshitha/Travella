-- Reference tables used by itinerary draft saves (run once against your Travella database).

CREATE TABLE IF NOT EXISTS tbl_meal_plans (
    id SERIAL PRIMARY KEY,
    name VARCHAR(120) NOT NULL
);

CREATE TABLE IF NOT EXISTS tbl_accommodations (
    id SERIAL PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    price_per_night NUMERIC(12, 2) NOT NULL DEFAULT 0
);

INSERT INTO tbl_meal_plans (name)
SELECT v FROM (VALUES ('BB'), ('HB'), ('FB'), ('AI')) AS t(v)
WHERE NOT EXISTS (SELECT 1 FROM tbl_meal_plans mp WHERE UPPER(TRIM(mp.name)) = UPPER(TRIM(t.v)));

INSERT INTO tbl_accommodations (name, price_per_night)
SELECT v, 0 FROM (VALUES ('hotel'), ('resort'), ('hostel'), ('villa'), ('apartment'), ('General')) AS t(v)
WHERE NOT EXISTS (SELECT 1 FROM tbl_accommodations ac WHERE LOWER(TRIM(ac.name)) = LOWER(TRIM(t.v)));

-- Backfill existing travelers so JWT includes companyId (adjust agency id as needed).
UPDATE tbl_users
SET company_id = COALESCE(company_id, 1)
WHERE role = 'TRAVELER' AND (company_id IS NULL OR company_id <= 0);

-- Staff availability uses BOOKED for itinerary assignment locks.
UPDATE tbl_staff_availability
SET status = 'BOOKED'
WHERE UPPER(COALESCE(status, '')) = 'LOCKED';

-- Ensure pricing table has full workflow costing fields.
ALTER TABLE tbl_itinerary_pricing
    ADD COLUMN IF NOT EXISTS driver_cost NUMERIC(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS guide_cost NUMERIC(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS vehicle_cost NUMERIC(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS mileage_rate NUMERIC(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_km NUMERIC(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS accommodation_cost NUMERIC(12,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS meal_plan VARCHAR(20) NOT NULL DEFAULT 'BB',
    ADD COLUMN IF NOT EXISTS profit_margin NUMERIC(12,2) NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS tbl_itinerary_messages (
    id SERIAL PRIMARY KEY,
    itinerary_id INT NOT NULL REFERENCES tbl_itineraries(id) ON DELETE CASCADE,
    sender_id INT NOT NULL,
    sender_role VARCHAR(20) NOT NULL CHECK (sender_role IN ('TRAVELER','STAFF','ADMIN')),
    message TEXT NOT NULL,
    type VARCHAR(30) NOT NULL CHECK (type IN ('REQUEST_CHANGE','COMMENT')),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_itinerary_messages_itinerary_created_at
    ON tbl_itinerary_messages(itinerary_id, created_at DESC);
