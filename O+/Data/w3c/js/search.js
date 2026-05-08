'use strict';
new QWebChannel(qt.webChannelTransport, function (channel) {
    window.bridge = channel.objects.bridge;
})
$(document).ready(function () {
    $(window).resize(function () {
        var view_height = $(window).height()-9
        $("#view_area").css("height", view_height - $(".page-header-ex").height()-$(".page-header-ex").offset().top)
    });
    $(document).bind("contextmenu",function(e){
        return false;
    });
});

function Init() {
}

function InitArgs() {
    $("#view_area").scrollTop(0);
}

function InitTrans(text1, text2){
    $(".s_page_title").text(text1)
    $(".no_content_text").text(text2)
}

function checkInit() {
    $(".card-header").each(function (index, value) {
        $(this).click(function(){
            var obj = $(this).children(".pull-right").children("span")
            var imei = $(this).children("span").attr("id");
            if (obj.hasClass('icon_up')) {
                obj.removeClass('icon_up')
                obj.addClass('icon_down')
            } else {
                obj.removeClass('icon_down')
                obj.addClass('icon_up')
                if (bridge) {
                    console.log(imei);
                    bridge.jsCallPC_getServiceRecordList(imei)
                }
            }
            $(this).siblings(".card-body").toggle();
            return false;
        });
    })
}

function registerEvent(){
    $(".row button").click(function () {
        console.log("repair: " + $(this).attr("data"))
        if (bridge) {
            // var title=$(this).parent().prev().find(".s_title_1").text().trim()
            var title = typeof($(this).attr("diag_type"))=="undefined" ? "": $(this).attr("diag_type")
            bridge.jsCallPC_detail($(this).attr("id"), $(this).attr("product"), $(this).attr("date_time"), $(this).attr("work_no"), $(this).attr("snapshot_id"), $(this).attr("device_type"), $(this).attr("device_project"), title);
        }
    })
}

function changePageStatus(type) {
    var page = $(".page-header-ex")
    var img = $("#img_state");
    var no_con = $(".no_content")
    if (type == 0) {
        page.hide()
        no_con.show()
        img.attr("src", "./images/no_result.png")
        img.css("margin-top", "0px")
        if (img.hasClass('rotate')) {
            img.removeClass("rotate");
        }
        $(".no_content_text").show()
        $(".card").each(function(){
            $(this).hide()
        })
        $("#view_area").hide()
    } else if (type == 1) {
        page.hide()
        no_con.show()
        img.attr("src", "./images/load.png")
        img.css("margin-top", "100px")
        if (!img.hasClass('rotate')) {
            img.addClass("rotate");
        }
        $(".no_content_text").hide()
        $(".card").each(function(){
            $(this).hide()
        })
        $("#view_area").hide()
    } else {
        page.show()
        if (img.hasClass('rotate')) {
            img.removeClass("rotate");
        }
        img.css("margin-top", "0px")
        no_con.hide()
        $(".card").each(function(){
            $(this).show()
        })
        $("#view_area").show()
        $("#view_area").css("height", $(window).height()-9- $(".page-header-ex").height()-$(".page-header-ex").offset().top)
    }
}

function clickDetail(serino) {

}

function navigateToDiagitem(category, itemNo){
    var anchor_point = $("span[id='"+itemNo+"']")
    var t_parent = anchor_point.parent().next()
    if(t_parent.css('display') == 'none'){
        t_parent.prev().click()
    }
    $("<a href='#"+itemNo+"'/>")[0].click()
}

function updateRepairResult(successlist, faillist) {
}

function repairAll() {
}

Init()
checkInit()