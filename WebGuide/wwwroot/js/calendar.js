document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');

    

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        locale: 'uk',
        height: 'auto',
        events: '/Tasks/GetEvents',

        eventDidMount: function (info) {
            info.el.style.backgroundColor = info.event.backgroundColor;
            info.el.style.color = info.event.extendedProps.textColor;
            info.el.style.border = 'none';
        },

        eventTimeFormat: {
            hour: '2-digit',
            minute: '2-digit',
            meridiem: false
        }
    });
    calendar.render();
});
