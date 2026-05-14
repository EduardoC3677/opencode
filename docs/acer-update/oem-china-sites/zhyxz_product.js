$(function () {
    // 购买商品
    function purchaseProductClick() {
        $('.btn-ceo-purchase-product').off('click').click(function () {
            let btnLoading = Ladda.create(this);
            btnLoading.start()
            let checkInterval = null
            let _UIKit = UIkit
            let product_id = $(this).data('product-id')
            let flush = $(this).data('flush')

            // 付费解锁，消耗下载次数打开
            if ($(this).data('type') == 'open') {
                $.post(zongcai.ajaxurl, {
                    action: 'ceo_shop_pay_product_download',
                    product_id: product_id,
                    sku: $(this).data('sku')
                }, function (res) {
                    btnLoading.stop()
                    btnLoading.remove()
                    if (res.success) {
                        _UIKit.notification(res.data, { status: 'success' })
                        window.setTimeout(function () {
                            window.location.reload()
                        }, 1500)
                    } else {
                        _UIKit.notification(res.data, { status: 'warning' })
                    }
                })
                return
            }

            let url = zongcai.ajaxurl + '?action=ceo_shop_pay_product&product_id=' + product_id
            if ($(this).data('sku')) {
                url += '&sku=' + $(this).data('sku')
            }
            if ($(this).data('type') == 'download') {
                url += '&download=1'
            }

            $.ajax({
                url: url,
                method: 'GET',
                success: function (res) {
                    // 弹出框展示
                    $('#ceoshop-member-box').html(res)
                    btnLoading.stop()
                    btnLoading.remove()
                    _UIKit.modal('#ceo-purchase-pay').show()
                    _UIKit.util.on('#ceo-purchase-pay', 'hidden', function () {
                        $('#ceoshop-member-box').html()
                        $('#ceo-purchase-pay').remove()
                    });
                    // 点击下载
                    $('#submit-ceo-product3').click(function () {
                        let btnLoading = Ladda.create(this);
                        btnLoading.start()
                        $.ajax({
                            url: zongcai.ajaxurl + '?action=ceo_shop_pay_product_download&product_id=' + product_id + '&sku=' + $('#ceo-product-sku').val(),
                            method: 'GET',
                            success: function (res) {
                                $('#ceoshop-member-box').html(res)
                                _UIKit.modal('#ceo-download').show()
                                _UIKit.util.on('#ceo-download', 'hidden', function () {
                                    $('#ceoshop-member-box').html()
                                    $('#ceo-download').remove()
                                });
                                btnLoading.stop()
                                btnLoading.remove()
                            }
                        })
                    })

                    // 购买数量
                    $('#ceo-purchase-pay .increase').click(function () {
                        // 计算应支付价格
                        $('#ceo-product-inventory').val(parseInt($('#ceo-product-inventory').val()) + 1)
                        $('#ceo-product-price').val(Decimal.mul(parseFloat($('#ceo-product-sku-price').val()), parseInt($('#ceo-product-inventory').val())).toFixed(2, Decimal.ROUND_DOWN))
                        // 计算实际支付价格并显示
                        calcRealPay()
                        // 计算VIP折扣优惠
                        calcVipDiscount()
                    })
                    $('#ceo-purchase-pay .decrease').click(function () {
                        // 计算应支付价格
                        let num = parseInt($('#ceo-product-inventory').val())
                        if (num - 1 < 1) {
                            $('#ceo-product-inventory').val(1)
                        } else {
                            $('#ceo-product-inventory').val(num - 1)
                        }
                        $('#ceo-product-price').val(Decimal.mul(parseFloat($('#ceo-product-sku-price').val()), parseInt($('#ceo-product-inventory').val())).toFixed(2, Decimal.ROUND_DOWN))
                        // 计算实际支付价格并显示
                        calcRealPay()
                        // 计算VIP折扣优惠
                        calcVipDiscount()
                    })

                    // 套餐选择
                    $('.product-sku-select').click(function () {
                        $('.product-sku-select').removeClass('select')
                        $(this).addClass('select')

                        // 切换展示当前套餐价格和库存
                        $('#product-current-price').text($(this).data('ori-price'))
                        $('#product-current-inventory').text($(this).data('inventory'))
                        $('#product-current-freight').text($(this).data('freight'))

                        // 设置当前选择SKU信息
                        $('#ceo-product-freight').val($(this).data('freight'))
                        $('#ceo-product-sku').val($(this).data('sku'))
                        $('#ceo-product-sku-discount').val($(this).data('discount'))
                        $('#ceo-product-sku-price').val($(this).data('price'))
                        $('#ceo-product-sku-ori-price').val($(this).data('ori-price'))
                        // 继续购买则使用原价
                        if ($(this).data('download') == -1) {
                            $('#ceo-product-sku-price').val($(this).data('ori-price'))
                        }

                        // 计算VIP折扣优惠
                        calcVipDiscount()

                        // 计算应支付价格
                        $('#ceo-product-price').val(Decimal.mul(parseFloat($('#ceo-product-sku-price').val()), parseInt($('#ceo-product-inventory').val())).toFixed(2, Decimal.ROUND_DOWN))
                        // 计算实际支付价格并显示
                        calcRealPay()

                        // 允许下载，展示下载按钮
                        if ($(this).data('download') == '1' || $(this).data('download') == '2') {
                            $('#submit-ceo-product').hide()
                            $('#submit-ceo-product2').hide()
                            $('#submit-ceo-product3').show()
                        } else {
                            // 无权下载，展示升级VIP
                            if ($(this).data('exclusive') == '1' && $('#ceo-user-vip').val() == -1) {
                                $('#submit-ceo-product').hide()
                                $('#submit-ceo-product2').show()
                                $('#submit-ceo-product3').hide()
                            } else {
                                $('#submit-ceo-product').show()
                                $('#submit-ceo-product2').hide()
                                $('#submit-ceo-product3').hide()
                            }
                        }

                        // 是否展示下载达上限继续购买提示
                        if ($(this).data('download') == '-1') {
                            $('.submit-ceo-product-tip').show()
                        } else {
                            $('.submit-ceo-product-tip').hide()
                        }
                    })

                    // 支付方式
                    $('.product-pay-payment').click(function () {
                        $('.product-pay-payment').removeClass('select')
                        $(this).addClass('select')
                        $('#ceo-product-payment').val($(this).data('payment'))
                        calcRealPay()
                    })

                    // 使用代金券
                    $('#product-use-coupon').click(function () {
                        let btnLoading = Ladda.create(this);
                        btnLoading.start()
                        let code = $('#product-use-coupon-code').val()
                        $.ajax({
                            url: zongcai.ajaxurl + '?action=ceo_shop_pay_discount_coupon&code=' + code + '&product_id=' + product_id,
                            method: 'GET',
                            success: function (res) {
                                btnLoading.stop()
                                btnLoading.remove()
                                if (res.success) {
                                    $('#ceo-product-discount').val(parseFloat(res.data.money))
                                    $('#ceo-product-discount-condition').val(parseFloat(res.data.condition_money))
                                    $('#product-show-discount-code').html('<span>' + res.data.title + '</span><em>- ' + res.data.money + '</em>')
                                    calcRealPay(true)
                                } else {
                                    $('#ceo-product-discount').val('0.00')
                                    $('#ceo-product-discount-condition').val('0.00')
                                    $('#product-show-discount-code').html('')
                                    $('#product-use-coupon-code').val('')
                                    calcRealPay()
                                    _UIKit.notification(res.data, { status: 'warning' })
                                }
                            },
                            error: function () {
                                console.log('检测订单支付状态时发生错误！')
                            }
                        });
                    })

                    // 购买商品提交
                    $('#submit-ceo-product').click(function () {
                        let btnLoading = Ladda.create(this);
                        btnLoading.start()
                        const data = {
                            action: 'ceo_shop_pay_product',
                            payment: $('#ceo-product-payment').val(),
                            product_id: product_id,
                            sku: $('#ceo-product-sku').val(),
                            inventory: $('#ceo-product-inventory').val(),
                            coupon_code: $('#product-use-coupon-code').val(),
                            buyer_mail: $('#ceo-product-mail').val(),
                            buyer_addr: $('#ceo-product-buyer-addr').val(),
                            buyer_remark: $('#ceo-product-buyer-remark').val(),
                            nonce: $('#ceo-product-nonce').val(),
                        }
                        $.post(zongcai.ajaxurl, data, function (res) {
                            if (res.success) {
                                if (res.data.pay_status == 0) {
                                    if (res.data.pay_url) {
                                        if (/Mobi|Android|iPhone/i.test(navigator.userAgent)) {
                                            window.location.href = res.data.pay_url
                                        } else {
                                            window.open(res.data.pay_url)
                                        }
                                    }
                                    if (res.data.jssdk) {
                                        if (typeof WeixinJSBridge == "undefined") {
                                            if (document.addEventListener) {
                                                document.addEventListener('WeixinJSBridgeReady', onBridgeReady, false);
                                            } else if (document.attachEvent) {
                                                document.attachEvent('WeixinJSBridgeReady', onBridgeReady);
                                                document.attachEvent('onWeixinJSBridgeReady', onBridgeReady);
                                            }
                                        } else {
                                            WeixinJSBridge.invoke(
                                                'getBrandWCPayRequest', res.data.jssdk,
                                                function (res) {
                                                    if (res.err_msg == "get_brand_wcpay_request:ok") {
                                                    }
                                                });
                                        }
                                    }
                                    if (res.data.qr_code) {
                                        // 弹出框展示
                                        $('#ceoshop-member-box').html(res.data.html)
                                        _UIKit.modal('#ceo-payment-wrap').show()
                                        _UIKit.util.on('#ceo-payment-wrap', 'hidden', function () {
                                            $('#ceoshop-member-box').html()
                                            $('#ceo-payment-wrap').remove()
                                        });
                                    }
                                    setTimeout(function () {
                                        check(res.data.order_sn, data.sku);
                                    }, 1000);
                                } else {
                                    btnLoading.stop()
                                    btnLoading.remove()
                                    UIkit.notification('订单支付成功！', { status: 'success' })
                                    window.setTimeout(function () {
                                        if (flush == 1) {
                                            // $('.btn-ceo-purchase-product').trigger('click')
                                            openDownloadPage(product_id, data.sku)
                                        } else {
                                            // 保存当前购买的SKU，刷新后恢复（仅非合集付费）
                                            var isGroup = $('#ceo-video-is-group').val();
                                            if(!isGroup && data.sku && data.sku > 1) {
                                                sessionStorage.setItem('ceo_video_current_sku', data.sku);
                                            }
                                            window.location.reload()
                                        }
                                    }, 1500)
                                }
                            } else {
                                btnLoading.stop()
                                btnLoading.remove()
                                UIkit.notification(res.data, { status: 'warning' })
                            }
                        })
                    })
                }
            })

            /**
             * 计算VIP折扣优惠
             */
            function calcVipDiscount() {
                // 原价展示 = sku原始单价 * 库存
                $('#product-ori-total').text(
                    Decimal.mul(
                        $('#product-current-price').text(),
                        $('#ceo-product-inventory').val()
                    ).toFixed(2, Decimal.ROUND_DOWN) + " " + $('#ceo-currency-name').val()
                )

                // 全部等级VIP折扣
                $("#ceo-product-all-vip-discount li").each(function () {
                    let tempVipDiscount = parseFloat($(this).data('discount-arr').split(',')[$('#ceo-product-sku').val() - 1]);
                    let tempVipDiscountPrice = Decimal.mul(
                        $('#product-current-price').text(),
                        $('#ceo-product-inventory').val()
                    )
                        .mul(tempVipDiscount)
                        .div(10)
                        .toFixed(2, Decimal.ROUND_DOWN);
                    if (tempVipDiscount == 0) {
                        $(this).children('p').html('免费')
                    } else {
                        $(this).children('p').html('<em>' + tempVipDiscountPrice + " " + $('#ceo-currency-name').val() + '</em>' + tempVipDiscount + '折')
                    }
                })

                // VIP折扣优惠
                if ($('#ceo-user-vip').val() != -1 && $('#ceo-product-sku-discount').val() != 10) {
                    // (sku原价 - sku折扣价) *  数量
                    let tempDiscountPrice = Decimal.mul(
                        parseInt($('#ceo-product-inventory').val()),
                        Decimal.sub(
                            parseFloat($('#ceo-product-sku-ori-price').val()),
                            parseFloat($('#ceo-product-sku-price').val())
                        )
                    ).toFixed(2, Decimal.ROUND_DOWN)

                    if ($('#ceo-product-sku-discount').val() == 0) {
                        $('#product-show-discount-vip').html('<span>' + $('#ceo-user-vip-name').val() + '免费</span><em>- ' + tempDiscountPrice + " " + $('#ceo-currency-name').val() + '</em>')
                    } else {
                        $('#product-show-discount-vip').html('<span>' + $('#ceo-user-vip-name').val() + '优惠' + $('#ceo-product-sku-discount').val() + '折</span><em>- ' + tempDiscountPrice + " " + $('#ceo-currency-name').val() + '</em>')
                    }
                }
            }

            /**
             * 计算实际支付价格并显示
             * 实付价格： (应付 - 折扣) / 比例 + 运费
             */
            function calcRealPay(tip = false) {
                let tempTotalPrice = parseFloat($('#ceo-product-price').val())

                // 使用优惠券
                if (parseFloat($('#ceo-product-discount').val()) > 0) {
                    let sumPrice = Decimal.add(
                        parseFloat($('#ceo-product-price').val()),
                        parseFloat($('#ceo-product-freight').val())
                    ).toFixed(2, Decimal.ROUND_DOWN);

                    if (parseFloat(sumPrice) < parseFloat($('#ceo-product-discount-condition').val())) {
                        _UIKit.notification('此代金券最低使用门槛' + $('#ceo-product-discount-condition').val(), { status: 'warning' })

                        $('#ceo-product-discount').val('0.00')
                        $('#ceo-product-discount-condition').val('0.00')
                        $('#product-show-discount-code').html('')
                        $('#product-use-coupon-code').val('')
                    } else {
                        if (tip) {
                            _UIKit.notification('代金券使用成功', { status: 'success' })
                        }

                        tempTotalPrice = Decimal.sub(
                            parseFloat($('#ceo-product-price').val()),
                            parseFloat($('#ceo-product-discount').val())
                        ).toFixed(2, Decimal.ROUND_DOWN)
                    }
                }


                if ($('#ceo-product-payment').val() != 'balance') {
                    tempTotalPrice = Decimal.div(
                        parseFloat(tempTotalPrice),
                        parseFloat($('#ceo-product-proportion').val())
                    ).toFixed(2, Decimal.ROUND_DOWN)
                }

                tempTotalPrice = Decimal.add(
                    parseFloat(tempTotalPrice),
                    parseFloat($('#ceo-product-freight').val())
                ).toFixed(2, Decimal.ROUND_DOWN)
                if (tempTotalPrice < 0) {
                    tempTotalPrice = '0.00'
                }

                if ($('#ceo-product-payment').val() != 'balance') {
                    $('#ceo_price').html('￥<span id="ceo-product-total">' + tempTotalPrice + '</span>')
                } else {
                    $('#ceo_price').html('&nbsp;<span id="ceo-product-total">' + tempTotalPrice + '</span> ' + $('#ceo-currency-name').val())
                }
            }

            function check(orderSn, sku) {
                $.ajax({
                    url: zongcai.ajaxurl + '?action=ceo_shop_pay_product_check&order_sn=' + orderSn,
                    method: 'GET',
                    success: function (res) {
                        if (res.success) {
                            UIkit.notification('订单支付成功！', { status: 'success' })
                            window.setTimeout(function () {
                                if (flush == 1) {
                                    // $('.btn-ceo-purchase-product').trigger('click')
                                    openDownloadPage(product_id, sku)
                                } else {
                                    // 保存当前购买的SKU，刷新后恢复（仅非合集付费）
                                    var isGroup = $('#ceo-video-is-group').val();
                                    if(!isGroup && sku && sku > 1) {
                                        sessionStorage.setItem('ceo_video_current_sku', sku);
                                    }
                                    window.location.reload()
                                }
                            }, 1500)
                        } else {
                            console.log('订单支付仍在进行中...')
                            setTimeout(function () {
                                check(orderSn, sku);
                            }, 1000);
                        }
                    },
                    error: function () {
                        console.log('检测订单支付状态时发生错误！')
                        setTimeout(function () {
                            check(orderSn, sku);
                        }, 1000);
                    }
                });
            }
        })
    }
    window.purchaseProductClick = purchaseProductClick;
    purchaseProductClick();

    // 页面加载时恢复购买的SKU位置（仅针对非合集付费的视频课程）
    function restoreVideoSku() {
        var savedSku = sessionStorage.getItem('ceo_video_current_sku');
        if (savedSku && savedSku > 1) {
            var $videoBtn = $('.btn-ceo-purchase-change-video[data-sku="' + savedSku + '"]').first();
            if ($videoBtn.length > 0) {
                setTimeout(function() {
                    $videoBtn.trigger('click');
                    sessionStorage.removeItem('ceo_video_current_sku');
                }, 500);
            } else {
                sessionStorage.removeItem('ceo_video_current_sku');
            }
        }
    }
    
    // 执行恢复逻辑
    restoreVideoSku();

    function openDownloadPage(product_id, sku_id) {
        $.ajax({
            url: zongcai.ajaxurl + '?action=ceo_shop_pay_product_download&product_id=' + product_id + '&sku=' + sku_id,
            method: 'GET',
            success: function (res) {
                $('#ceoshop-member-box').html(res)
                UIkit.modal('#ceo-download').show()
                UIkit.util.on('#ceo-download', 'hidden', function () {
                    $('#ceoshop-member-box').html()
                    $('#ceo-download').remove()
                });
            }
        })
    }


    // 切换视频
    $('.btn-ceo-purchase-change-video').off('click').click(function () {
        let product_id = $(this).data('product-id')
        let sku = $(this).data('sku')
        let _this = $(this)

        $.ajax({
            url: zongcai.ajaxurl + '?action=ceo_shop_pay_change_video&product_id=' + product_id + '&sku=' + sku,
            method: 'GET',
            success: function (res) {
                $('#ceo-video-s').html(res)

                purchaseProductClick()

                $('.ceo-video-list-xinxi-down .btn-ceo-purchase-product').data('sku', sku)

                $(".ceo-video-list-liebiao-box li div").removeClass('ceo-video-list-liebiao-s')
                $(".ceo-video-list-liebiao-box li div a").removeClass('ceo-video-list-liebiao-d')
                $(".ceo-video-list-liebiao-box li div i").removeClass().addClass('ceofont icon-play')
                if (_this.hasClass('video_link_play')) {
                    _this.prev().addClass('ceo-video-list-liebiao-d')
                    _this.prev().children().removeClass().addClass('icon-video')
                    _this.addClass('ceo-video-list-liebiao-d')
                } else {
                    _this.addClass('ceo-video-list-liebiao-d')
                    _this.children().removeClass().addClass('icon-video')
                    _this.next().addClass('ceo-video-list-liebiao-d')
                }

                if (jQuery('.ckplayer-video').length) {
                    jQuery('.ckplayer-video').each(function () {
                        jQuery(this).height(jQuery(this).width() * 0.5678);
                    });
                }

                if (jQuery(".ckplayer-video-real").length) {
                    jQuery(".ckplayer-video-real").bind('contextmenu', function () {
                        return false;
                    });
                    jQuery(".ckplayer-video-real").each(function () {
                        var conv = jQuery(this).data("video")
                        conn = jQuery(this).data("nonce");
                        console.log(conn, conv)

                        new ckplayer({
                            container: "#ckplayer-video-" + conn,
                            variable: "player" + conn,
                            autoplay: false,
                            video: conv
                        });
                    });
                }

                //dplayer
                if (jQuery(".dplayer-video-real").length) {
                    jQuery(".dplayer-video-real").bind('contextmenu', function () {
                        return false;
                    });
                    jQuery(".dplayer-video-real").each(function () {
                        var conv = jQuery(this).data("video")
                        conn = jQuery(this).data("nonce");
                        if (jQuery(this).hasClass("video-blob")) {
                            jQuery(".article-video").append("<div class='article-video-loading' style='z-index: 9;position: absolute;top: calc(50% - 12px);left: 0;right: 0;color: #fff;font-size: 16px;text-align: center;'>视频加载中...</div>");
                            window.URL = window.URL || window.webkitURL;
                            var xhr = new XMLHttpRequest();
                            // xhr.open("GET", Base64.decode(conv), true);
                            xhr.open("GET", conv, true);
                            xhr.responseType = "blob";
                            xhr.timeout = 1000 * 60 * 1;
                            xhr.onload = function (e) {
                                if (this.status == 200) {
                                    var blob = this.response;
                                    jQuery(".article-video-loading").remove();
                                    new DPlayer({
                                        container: document.getElementById("dplayer-video-" + conn),
                                        screenshot: true,
                                        autoplay: false,
                                        video: {
                                            url: window.URL.createObjectURL(blob),
                                            pic: '',
                                            thumbnails: '',
                                            type: 'auto'
                                        }
                                    });
                                }
                            }
                            xhr.send();
                        } else {
                            new DPlayer({
                                container: document.getElementById("dplayer-video-" + conn),
                                screenshot: true,
                                autoplay: false,
                                video: {
                                    url: conv,
                                    pic: '',
                                    thumbnails: '',
                                    type: 'auto'
                                }
                            });
                        }
                    });
                }

            }
        })
    })
})