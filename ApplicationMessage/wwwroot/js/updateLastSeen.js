async function updateLastSeen() {
    await fetch('/Account/UpdateLastSeen', { method: 'POST' });
}

// Call every 1 minute
setInterval(updateLastSeen, 60000);
