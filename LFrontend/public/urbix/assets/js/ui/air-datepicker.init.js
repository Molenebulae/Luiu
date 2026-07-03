const localeEn = {
    days: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],
    daysShort: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"],
    daysMin: ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"],
    months: ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
    monthsShort: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
    today: "Today",
    clear: "Clear",
    dateFormat: "MM/dd/yyyy",
    timeFormat: "hh:ii aa",
    firstDay: 0
};
const localeZh = {
    days: ["星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六"],
    daysShort: ["週日", "週一", "週二", "週三", "週四", "週五", "週六"],
    daysMin: ["日", "一", "二", "三", "四", "五", "六"],
    months: ["一月", "二月", "三月", "四月", "五月", "六月", "七月", "八月", "九月", "十月", "十一月", "十二月"],
    monthsShort: ["1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月"],
    today: "今天",
    clear: "清除",
    dateFormat: "yyyy/MM/dd",
    timeFormat: "HH:mm",
    firstDay: 0
};
new AirDatepicker(
    "#basic-picker", { autoClose: !1, dateFormat: "dd/MM/yyyy", locale: localeEn }
),
    new AirDatepicker(
        "#select-initialization-piker", { selectedDates: [new Date], locale: localeEn }
    ),
    new AirDatepicker(
        "#month-selection-picker", { view: "months", minView: "months", dateFormat: "MMMM yyyy", locale: localeEn }
    ),
    new AirDatepicker(
        "#mobile-devices-picker", { isMobile: !0, autoClose: !0, locale: localeEn }
    ),
    new AirDatepicker(
        "#positioning-picker", { position: "right center", locale: localeEn }
    ),
    new AirDatepicker(
        "#range-picker", { range: !0, multipleDatesSeparator: " - ", locale: localeEn }
    ),
    new AirDatepicker("#timepicker", { timepicker: !0, locale: localeEn }

    ),
    new AirDatepicker(
        "#BirthdayPicker-zh", { locale: localeZh, autoClose: true, maxDate: new Date() }
    );
let today = new Date;
new AirDatepicker(
    "#cells-picker", {
    onRenderCell({ date: e, cellType: a }) {
        let t = ["💕", "😃", "🍙", "🍣", "🍻", "🎉", "🥁"],
            l = "day" === a, r = e.getDate(),
            i = l && [1, 5, 7, 10, 15, 20, 25].includes(r),
            o = t[Math.floor(Math.random() * t.length)];
        return {
            html: i ? o : void 0,
            classes: i ? "-emoji-cell-" : void 0,
            attrs: { title: i ? o : "" }
        }
    },
    locale: localeEn,
    selectedDates: new Date(today.getFullYear(), today.getMonth(), 10)
}
),
    new AirDatepicker("#preinstalled-picker", { buttons: ["today", "clear"], locale: localeEn });