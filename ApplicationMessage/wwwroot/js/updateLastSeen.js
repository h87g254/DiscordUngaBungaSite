async function updateLastSeen() {
    await fetch('/Account/UpdateLastSeen', { method: 'POST' });
}

// Call every 2 minutes
setInterval(updateLastSeen, 120000);
