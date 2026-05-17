-- Run once: assigned reviewer + internal staff notes for itinerary chat.

ALTER TABLE tbl_itineraries
    ADD COLUMN IF NOT EXISTS assigned_reviewer_id INT NULL REFERENCES tbl_users(id);

CREATE INDEX IF NOT EXISTS idx_itineraries_assigned_reviewer
    ON tbl_itineraries(assigned_reviewer_id);

ALTER TABLE tbl_itinerary_messages DROP CONSTRAINT IF EXISTS tbl_itinerary_messages_type_check;

ALTER TABLE tbl_itinerary_messages
    ADD CONSTRAINT tbl_itinerary_messages_type_check
    CHECK (type IN ('REQUEST_CHANGE', 'COMMENT', 'INTERNAL_NOTE'));
