#!/bin/bash

# Constants
HOST="ca-server.local"
PORT="9091"
USER="dick"
PASS="a"
LOG_FILE="/Users/dick/TVMaze/Logs/Transmission.log"
DATE_FORMAT="+%Y-%m-%d %H:%M:%S"

# Function to log messages
log_message() {
    local message="$1"
    echo "$(date "$DATE_FORMAT") - $message" >> "$LOG_FILE"
}

# Function to get transmission session ID
get_transmission_session_id() {
    curl --silent --anyauth --user "$USER:$PASS" "http://$HOST:$PORT/transmission/rpc" |
    sed 's/.*<code>//g;s/<\/code>.*//g'
}

# Function to add torrent
add_torrent() {
    local session_id="$1"
    local link="$2"
    curl --silent --anyauth --user "$USER:$PASS" --header "$session_id" \
        "http://$HOST:$PORT/transmission/rpc" \
        -d "{\"method\":\"torrent-add\",\"arguments\":{\"paused\":${PAUSED},\"filename\":\"${link}\"}}" \
        >> "$LOG_FILE" 2>> "$LOG_FILE"
}

# Main Script
if [ -z "$1" ]; then
    echo "Usage: $0 <magnet link>"
    exit 1
fi

LINK="$1"
PAUSED="false"

log_message "Starting script with magnet link: $LINK"

SESSID=$(get_transmission_session_id)

if [ -z "$SESSID" ]; then
    log_message "Failed to obtain Transmission session ID."
    echo "Failed to connect to the Transmission RPC. Check your credentials."
    exit 1
fi

add_torrent "$SESSID" "$LINK"

log_message "Magnet link added to Transmission: $LINK"
