'use strict';
new QWebChannel(qt.webChannelTransport, function (channel) {
    window.bridge = channel.objects.bridge;
})

var show_max_count = 3

$(document).ready(function () {
    document.body.onkeydown = function (event) {
        if (window.event) {
            return false;
        }
    }
});

function toggleItem(lst, is_hide){
    var items = lst.slice(show_max_count+1, lst.length-1)
    if(is_hide){
        $.each(items,function(i, val){
        $(val).hide();
        });
    } else {
        $.each(lst,function(i, val){
        $(val).show();
        });
    }
}

function Init(){
    $("#container").delegate(".btn_custom", "click", function () {
        var uuid = $(this).attr("uuid");
        var name = $(this).attr("name");
        var is_local = $(this).attr("is_local");
        var opt = $(this).attr("type");
        if (bridge) {
            bridge.jsCallPC_clickItem(name, uuid, is_local, opt);
        }
    });
    $("#container").delegate(".btn_flash", "click", function () {
        var uuid = $(this).attr("uuid");
        var name = $(this).attr("name");
        var is_local = $(this).attr("is_local");
        var opt = $(this).attr("type");
        if (bridge) {
            bridge.jsCallPC_clickItem(name, uuid, is_local, opt);
        }
    });
    $("#container").delegate(".progress", "click", function () {
        var uuid = $(this).attr("uuid");
        var name = $(this).attr("name");
        var is_local = $(this).attr("is_local");
        var opt = $(this).attr("type");
        if (bridge) {
            bridge.jsCallPC_clickItem(name, uuid, is_local, opt);
        }
    });
    $("#container").delegate(".pull-left", "click", function () {
        var uuid = $(this).next().children("button").attr("uuid");
        if (bridge) {
            bridge.jsCallPC_upgradeInfo(uuid);
        }
    });
    $("#container").delegate(".btn_more", "click", function () {
        var thumbnail = $(this).parent().parent();
        var lst = thumbnail.children("div");
        var icon = $(this).children("span")[2];
        var more = $(this).children("span")[0];
        var packup = $(this).children("span")[1];
        if ($(icon).hasClass('icon_down')) {
            $(icon).removeClass('icon_down')
            $(icon).addClass('icon_up')
            $(more).hide()
            $(packup).show()
            toggleItem(lst, false)
        } else {
            $(icon).removeClass('icon_up')
            $(icon).addClass('icon_down')
            $(packup).hide()
            $(more).show()
            toggleItem(lst, true)
        }
    });
}


function checkInit() {
    $(".no_content").remove();
}

function navigateToDiagitem(itemNo){
    $("<a href='#point_"+itemNo+"'/>")[0].click()
}

function removeItem(itemNo, content){
    var flashObj = $("button[uuid="+itemNo+"][class=btn_flash]");
    flashObj.hide();
    var itemObj = $("button[uuid="+itemNo+"][class=btn_custom]");
    var local = itemObj.attr("is_local")
    if(local=="1"){
        var dlvItemObj = $("span[id='point_" + itemNo + "']").parent()
        var dlvColors = dlvItemObj.parent()
        dlvItemObj.remove()
        if(dlvColors.children("div[type='pkg']").length == 0){
            dlvColors.parent().remove()
        }
    }else{
        $(itemObj).text(content);
    }
}

function addItem(coloros, html){
    $(".title").each(function(index, domele){
        if($(domele).text() == coloros){
            var arr = $(domele).parent().parent().children("div[type='pkg']")
            $(arr[arr.length-1]).after(html)
        }
    })
}

function setProgress(itemNo, content, type) {
    var itemObj = $("button[uuid=" + itemNo + "][class=btn_custom]");
    var labDecompressObj = $("span[uuid="+itemNo+"]");
    var progressObj = $("div[uuid=" + itemNo + "][class=progress]");
    progressObj.show()
    itemObj.hide()
    $(progressObj).attr("type", type);
    var pObj = progressObj.children("p");
    progressObj.css("margin-top", "0px");
    if (type == "2") {
        labDecompressObj.hide();
        itemObj.show();
        $(itemObj).attr("type", 0);
        var val = content;
        pObj.text(val + "%")
        progressObj.css("--percent", val);
        progressObj.css("height", "28px");
    } else if(type == "3" || type == "8") {
        labDecompressObj.hide();
        itemObj.show();
        $(itemObj).attr("type", 0);
        var val = content;
        pObj.text(val)
        progressObj.css("--percent", "0");
        progressObj.css("height", "28px");
    } else if(type == "4") {
        labDecompressObj.hide();
        var val = content;
        pObj.text(val)
        progressObj.css("--percent", "100");
        progressObj.css("height", "28px");
    } else if(type == "5" || type == "11" || type == "9" || type == "10") {
        labDecompressObj.show();
        var val = content;
        progressObj.css("--percent", val);
        progressObj.css("height", "5px");
        progressObj.css("margin-top", "8px");
    } else if(type == "6" || type == "7"){
        labDecompressObj.hide();
        var val = content;
        pObj.text(val)
        progressObj.css("--percent", "100");
        progressObj.css("height", "28px");
    } else {
        labDecompressObj.hide();
        pObj.text(content)
    }
}

function setBtnText(itemNo, content, type){
    var itemObj = $("button[uuid="+itemNo+"][class=btn_custom]");
    var flashObj = $("button[uuid="+itemNo+"][class=btn_flash]");
    var labDecompressObj = $("span[uuid="+itemNo+"]");
    var progressObj = $("div[uuid=" + itemNo + "][class=progress]");
    if (type == "0"){
        flashObj.show()
        labDecompressObj.hide();
        progressObj.hide()
    } else {
        flashObj.hide()
    }
    if(type=="9" || type=="10"){
        $(labDecompressObj).text(content);
        labDecompressObj.show();
        itemObj.hide()
        progressObj.css("--percent", "0");
        progressObj.css("height", "5px");
    }else if(type=="0" || type=="1" || type=="6"|| type=="7" || type=="10"){
        $(itemObj).text(content);
        itemObj.show()
        progressObj.hide()
        labDecompressObj.hide();
        $(itemObj).attr("type", type);
    } else {
        setProgress(itemNo, content, type)
    }
}

Init()