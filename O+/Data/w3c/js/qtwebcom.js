'use strict';
new QWebChannel(qt.webChannelTransport, function (channel) {
    window.bridge = channel.objects.bridge;
})
function FixedBottomIsHidden(){
    if($(".navbar-fixed-bottom").is(':hidden')){
        return 10;
    }else{
        return 70;
    }
}
function AutoFixedViewArea(){
    var view_height = $(window).height();
    $("#view_area").css("height", view_height - $(".navbar").height()-FixedBottomIsHidden());
}
$(document).ready(function(){
    $(window).resize(function() {
        AutoFixedViewArea();
    });
    $(document).bind("contextmenu",function(e){
        return false;
    });
});

function Init() {
    $(".navbar-fixed-top li").each(function () {
        $(this).click(function () {
            $(this).addClass("nav_select");
            $(this).children("a").css("color", "rgba(0, 0, 0, 1)")
            $(this).siblings().removeClass("nav_select")
            $(this).siblings().children("a").css("color", "rgba(0, 0, 0, 0.55)")
            $(".card").each(function(){
                $(this).attr("show_tag", "")
            });
            if($(this).children("a").attr("href")=="#view_error"){
                ItemsFilter(".tt_err")
            }else if($(this).children("a").attr("href")=="#view_advice"){
                ItemsFilter(".tt_advise")
            }else if($(this).children("a").attr("href")=="#view_general"){
                ItemsFilter(".tt_normal")
            }else if($(this).children("a").attr("href")=="#view_uncheck"){
                ItemsFilter(".tt_nodetected")
            }else if($(this).children("a").attr("href")=="#view_guideline"){
                ItemsFilter(".tt_consult")
            }else{
                changePageStatus(3)
                $(".card-item").each(function(){
                    $(this).show()
                    $(this).next("p").show()
                });
                $(".card").each(function(){
                    $(this).show()
                });
                $(".err_sum").each(function(){
                    $(this).show()
                });
            }
            user_select_change($(this).children("a").contents()[0].nodeValue)
        })
    })
    $(".newnps-list li").bind("click", function () {
        $(this).css("border", "1px solid #2FC29B");
        $(this).css("background-color", "#EAF9F5");
        $(this).siblings().css("border", "1px solid #666666");
        $(this).siblings().css("background-color", "#FFF");
        if($(this).index() == 0){
            $(this).children("i").removeClass("icon_smile");
            $(this).children("i").addClass("icon_smile-clicked");
            $(this).siblings().children("i").removeClass("icon_cry-clicked");
            $(this).siblings().children("i").addClass("icon_cry");
        }else{
            $(this).children("i").removeClass("icon_cry");
            $(this).children("i").addClass("icon_cry-clicked");
            $(this).siblings().children("i").removeClass("icon_smile-clicked");
            $(this).siblings().children("i").addClass("icon_smile");
        }
        $('.navbar-fixed-bottom').stopTime();
        $('.navbar-fixed-bottom').oneTime('3s',function(){
            $(".navbar-fixed-bottom").animate({left: "10000px"}, 3000, function(){
                $(".navbar-fixed-bottom").hide();
                AutoFixedViewArea();
            });
        });
        var btn_type = $(this).index() + 1;
        if (bridge) {
            bridge.jsCallPC_reportDiscuss(btn_type);
        }
    });
}

function user_select_change(item_text){
    var item_count = $('.card-item').filter(function() {
        return $(this).css('display') !== 'none';
    }).length;
    if (bridge) {
        bridge.jsCallPc_UserSelectChange(item_text, ""+item_count);
    }
}

function ItemsFilter(tag){
    if(tag==".tt_err"){
        $(".err_sum").each(function(){
            $(this).show()
        });
    } else{
        $(".err_sum").each(function(){
            $(this).hide()
        });
    }
    var item_sum=0
    $(".card-item").each(function(){
        var list = $(this).find(tag)
        if(list.length==0){
            item_sum+=1
            $(this).hide()
            $(this).next("p").hide()
            $(this).prev("p").hide()
            if($(this).parent().parent().attr("show_tag")!="show"){
                $(this).parent().parent().hide()
            }
        }else{
            $(this).show()
            $(this).next("p").show()
            $(this).parent().parent().show()
            $(this).parent().parent().attr("show_tag", "show");
        }
    })
    if(item_sum==$(".card-item").length){
        changePageStatus(0)
    }else{
        changePageStatus(3)
    }
}

function changePageStatus(type){
    var img = $("#img_state");
    var no_con = $(".no_content")
    if(type==0){
        $(".navbar-fixed-top").show()
        no_con.show()
        img.attr("src", "./images/no_content.png")
        img.css("margin-top", "0px")
        if (img.hasClass('rotate')) {
            img.removeClass("rotate");
        }
        $(".no_content_text").show()
        $(".card").each(function(){
            $(this).hide()
        })
        $("#view_area").parent().hide()
    }else if(type==1){
        $(".navbar-fixed-top").hide()
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
        $("#view_area").parent().hide()
    }else if (type==2){
        $(".navbar-fixed-top").show()
        if (img.hasClass('rotate')) {
            img.removeClass("rotate");
        }
        no_con.hide()
        $(".card").each(function(){
            $(this).show()
        })
        $("#view_area").parent().show()
        AutoFixedViewArea()
    }else{
        $(".navbar-fixed-top").show()
        if (img.hasClass('rotate')) {
            img.removeClass("rotate");
        }
        no_con.hide()
        $("#view_area").parent().show()
        AutoFixedViewArea()
    }
}

function InitArgs(){
    $(".navbar-fixed-top span").each(function (){
        $(this).html("");
    })
    $("#view_area").scrollTop(0);
    $(".card").each(function(){
        $(this).remove()
    });
    $("#view_all").each(function(){
        $(this).remove()
    });
}

function checkInit(number) {
    $(".card-header").click(function () {
        var obj = $(this).children(".pull-right").children("span")
        if (obj.hasClass('icon_up')) {
            obj.removeClass('icon_up')
            obj.addClass('icon_down')
        } else {
            obj.removeClass('icon_down')
            obj.addClass('icon_up')
        }
        $(this).siblings(".card-body").toggle();
    })
    $(".btn_detail").click(function () {
        $(this).next().toggle();
        if ($(this).children("span").hasClass('icon_down')) {
            $(this).children("span").removeClass('icon_down')
            $(this).children("span").addClass('icon_up')
            if (bridge) {
                var imgNo = $(this).attr("data");
                if(imgNo!=""){
                    bridge.jsCallPC_drawImg(imgNo);
                    $(this).attr("data", "");
                }
            }
        } else {
            $(this).children("span").removeClass('icon_up')
            $(this).children("span").addClass('icon_down')
        }
    });
    $(".row button").click(function () {
        console.log("repair: " + $(this).attr("data"))
        setRepairStatus($(this), 1)
        if (bridge) {
            bridge.jsCallPC_repair($(this).attr("data"));
        }
    })
    $("div[tag$='expend']").each(function(){
        $(this).hide()
    })
    $(".card-item").delegate(".returnspan", "click", function () {
        $(this).parent().parent().parent().addClass("hide");
    });
    $(".card-item").delegate(".returnchart", "click", function () {
        $(this).parent().addClass("hide");
    });
    var nav_err = $("#itemno_"+number).parent().parent()
    nav_err.addClass("nav_select")
    nav_err.children("a").css("color", "rgba(0, 0, 0, 1)")
    nav_err.siblings().removeClass("nav_select")
    nav_err.siblings().children("a").css("color", "rgba(0, 0, 0, 0.55)")
    $(".navbar-fixed-bottom").show()
    $(".navbar-fixed-bottom").css("left", "0px");
    $(".newnps-list li").each(function(){
        $(this).css("border", "1px solid #666666");
        $(this).css("background-color", "#FFF");
        if($(this).index() == 0){
            $(this).children("i").removeClass("icon_smile-clicked");
            $(this).children("i").addClass("icon_smile");
        }else{
            $(this).children("i").removeClass("icon_cry-clicked");
            $(this).children("i").addClass("icon_cry");
        }
    });
}

function setItemCount(itemIndex, count)
{
    if (itemIndex in ["0", "1", "2", "4", "5"]) {
        $("#itemno_" + itemIndex).text("("+count+")")
    }
}

function navigateToDiagitem(category, itemNo){
    $(".navbar li a[href='"+category+"']")[0].click()
    var anchor_point = $("span[id='point_"+itemNo+"']")
    var t_parent = anchor_point.parent().parent()
    if(t_parent[0].tagName=="DIV"){
        if(t_parent.css('display') == 'none'){
            t_parent.prev().click()
        }
    }
    $("<a href='#point_"+itemNo+"'/>")[0].click()
}

function setRepairStatus(btnObj, status){
    var that = btnObj
    var divObj = that.parent().parent().parent()
    if(status==0){
        var img = divObj.find(".img_load")
        if(img.hasClass('rotate')){
           img.hide()
           img.removeClass("rotate");
        }
        that.attr("is_repair", true)
        that.hide()
        divObj.css("background", "#FBFBFB")
        divObj.find("h5 span:first").css("color", "#2DA74E")
        divObj.find("h5 span:first").text(getTranslate("entry_html_10"))
    }else if (status==1){
        var img = divObj.find(".img_load")
        if(!img.hasClass('rotate')){
           img.show()
           img.addClass("rotate");
        }
        that.hide()
    }else if (status == -1){
        var img = divObj.find(".img_load")
        if(img.hasClass('rotate')){
           img.hide()
           img.removeClass("rotate");
        }
        that.attr("is_repair", false)
        that.show()
        that.css("color", "#E32E27")
        that.css("background", "#F9DBDB")
        that.text(getTranslate("entry_html_11"))
        divObj.css("background", "#FDF5F5")
        divObj.find("h5 span:first").css("color", "#E32E27")
        divObj.find("h5 span:first").text(getTranslate("entry_html_12"))
    }
}

function updateRepairResult(successlist, faillist){
    $(".btn_repiar").each(function () {
        var repairstr = $(this).attr("data");
        var flag = 1;
        var repairlst = repairstr.split(",");
        for (var i = 0; i < repairlst.length; i++) {
            //judge items not in successlist,include fail now or before
            if ($.inArray(repairlst[i], successlist) < 0) {
                flag = 0;
                break;
            }
        }
        if (flag > 0) {
            setRepairStatus($(this), 0)
        }else{
            for (var i = 0; i < faillist.length; i++) {
                var failcode = faillist[i];
                if (repairstr.indexOf(failcode) >= 0 && failcode != '0') {
                    //repair fail now
                    setRepairStatus($(this), -1)
                }
            }
        }
    });
}

function repairAll(repairList){
    $(".btn_repiar").each(function () {
        var that = $(this);
        // setRepairStatus($(this), 1);
        var repairstr = that.attr("data");
        var repairlst = repairstr.split(",");
        for (var i = 0; i < repairlst.length; i++) {
            //judge items not in successlist,include fail now or before
            if ($.inArray(repairlst[i], repairList) >= 0) {
                if(that.attr('is_repair') == "false" || that.attr('is_repair') == false){
                    setRepairStatus($(this), 1);
                }
            }
        }
    })
}

function getDateDetail(id, date) {
    if (bridge) {
        bridge.jsCallPC_getDateDetail(id, date);
    }
}

Init()