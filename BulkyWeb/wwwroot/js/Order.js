var dataTable;

$(document).ready(function () {
    var queryString = window.location.search;
    if (queryString.includes("pending")) {
        loadDataTable("pending")
    } else if (queryString.includes("inprocess")) {
        loadDataTable("inprocess")
    } else if (queryString.includes("completed")) {
        loadDataTable("completed")
    } else if (queryString.includes("approved")) {
        loadDataTable("approved")
    } else {
        loadDataTable("")
    }




});

function loadDataTable(status) {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/Order/getall?status=' + status },
        "columns": [
            { "data": "orderHeaderId", "width": "5%" },
            { "data": "name", "width": "15%" },
            { "data": "phoneNumber", "width": "20%" },
            { "data": "applicationUser.email", "width": "15%" },
            { "data": "orderTotal", "width": "15%" },
            { "data": "orderStatus", "width": "15%" },
            {
                data: 'orderHeaderId',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                     <a href="/admin/order/details/${data}" class="btn btn-primary mx-2"> <i class="bi bi-pencil-square"></i></a>               
                    </div>`
                },
                "width": "25%"
            }

        ]
    });
}
