'use strict';
var progress = new ColorProgress('progress', 'success', 0, 100,5);
var types = ['exceed', 'success', 'warning', 'danger']
$("#result_table").delegate(".returnspan","click",function(){
    $(this).parent().parent().parent().addClass("hide");
});
$("#result_table").delegate(".returnchart","click",function(){
    $(this).addClass("hide");
});
//点击显示隐藏图标
$("#result_table").delegate(".showchart","click",function(){
    var storemessage = $(this).attr("storemessage");
    $(this).attr("storemessage",$(this).html());
    $(this).html(storemessage);
    var id=$('#'+$(this).attr("mountid"));
    var td=id.parent();
    if(td.hasClass("chart"))
    {
        td.removeClass("chart");
        td.addClass("charthide");
        td.children("div").addClass("hide");
        td.parents("tr").removeClass("shadow_bottom");
        $(this).parents(".tr_result").removeClass("shadow_top");
    }
    else
    {
        td.removeClass("charthide");
        td.addClass("chart");
        td.children("div:not(.returnchart)").removeClass("hide");
        td.parents("tr").addClass("shadow_bottom");
        $(this).parents(".tr_result").addClass("shadow_top");
        var charts =$(this).parent().parent().parent().next().children().children();
        for(var i=0;i<charts.length;i++)
        {
            var mountid = $(charts[i]).attr("id");
            if(mountid.indexOf("chart")!=-1)
            {
                window[mountid].changeWidth(window.innerWidth-60);
            }
        }
    }
});
//类别优先展开
$("#result_table").delegate(".typeTable th","click",function(){
    var span=$(this).children("span");
    if(span.hasClass("glyphicon-chevron-right")){//展开
        $(this).parent().parent().parent().next().removeClass("hide");
        span.removeClass("glyphicon-chevron-right");
        span.addClass("glyphicon-chevron-down");
    }else{//合并
        $(this).parent().parent().parent().next().addClass("hide");
        span.addClass("glyphicon-chevron-right");
        span.removeClass("glyphicon-chevron-down");
    }
});
//检测初始化
function checkinit(){
    $(".currentDevice").addClass("hide");
    $(".checkCount").addClass("hide");
    $(".checkHead").addClass("hide");
    $(".checkBody").addClass("hide");
}

//检测结束
function checkfinish(flag)
{
    $(".currentDevice").removeClass("hide");
    $(".checkCount").removeClass("hide");
    $(".checkHead").removeClass("hide");
    $(".checkBody").removeClass("hide");
    progress.setPercentage(100);
    $("#check_progress").attr("value",100);
    if(flag){
        $(".resultHead").removeClass("hide");
    }else{
        $(".resultHead").addClass("hide");
    }
}

function diagnosicType(Index){
    progress.setType(types[Index])
    return;
}

$("#show_type").change(function(){
    var value=$("#show_type option:selected").attr("value");
    if (bridge) {
        bridge.jsCallPC_sortChanged(value);
    }
});
$("#recordtimes").change(function(){
    var value=$("#recordtimes option:selected").attr("value");
    if(bridge){
        bridge.jsCallPC_currentRecordChanged(value);
    }
});
//删除记录
$("#btn_delete_single").click(function(){
    var value=$("#recordtimes option:selected").attr("value");
    var list=[];
    list.push(value);
    if(bridge){
        bridge.jsCallPC_deleteRecord(list, $('#imei').html(), "single");
    }
});
//删除设备记录
$("#btn_delete_device").click(function(){
    var options=$("#recordtimes option");
    var list=[];
    for(var i=0;i<options.length;i++){
        list.push($(options[i]).attr("value"));
    }
    if(bridge){
        bridge.jsCallPC_deleteRecord(list, $('#imei').html(), "multi");
    }
});

//qt交互
new QWebChannel(qt.webChannelTransport, function(channel) {
   window.bridge = channel.objects.bridge;
 })


