var attempts = 0;
document.getElementById('searchButton').addEventListener('click', function () {
    attempts++;
    var statusElem = document.getElementById('searchStatus');
    var btn = document.getElementById('searchButton');
    if (attempts === 1) {
        statusElem.textContent = "Searching...";
        setTimeout(function () {
            statusElem.textContent = "Nope, still missing.";
        }, 1500);
    } else if (attempts === 2) {
        statusElem.textContent = "Searching again...";
        setTimeout(function () {
            statusElem.textContent = "404 confirmed. It's gone.";
        }, 1500);
    } else if (attempts === 3) {
        statusElem.textContent = "Last attempt...";
        setTimeout(function () {
            statusElem.textContent = "Okay okay, it's not here. Stop poking me 😅";
        }, 1500);
        btn.disabled = true;
    }
});
