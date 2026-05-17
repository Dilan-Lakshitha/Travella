-- Notifications table for Travella real-time alerts
CREATE TABLE IF NOT EXISTS tbl_notifications (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES tbl_users(id) ON DELETE CASCADE,
    itinerary_id INT NULL REFERENCES tbl_itineraries(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON tbl_notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_user_unread ON tbl_notifications(user_id, is_read) WHERE is_read = false;
