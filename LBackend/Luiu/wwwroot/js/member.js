$(document).ready(function () {
    console.log("ready");
    $(".search-type-option").on("click", function (e) {
        e.preventDefault();

        let type = $(this).data("type");
        let content = $(this).html();

        $("#search-type-btn").html(content);

        $("#searchCategory").val(type);

    });
});