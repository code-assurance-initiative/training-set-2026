// Saves a booking as an .ics file the visitor can open in their own calendar application.

window.bookingExport = {
    saveCalendarFile(fileName, text) {
        const blob = new Blob([text], { type: "text/calendar;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    },
};
