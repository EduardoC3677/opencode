let loginSuccessRedirectUrl = ''

function thirdPartyLogin(provider, oauthConfig) {
    let url = `${oauthConfig.endpoint}/oauth2/authorize?redirect_uri=${oauthConfig.callbackUrl}&response_type=code&client_id=${oauthConfig.clientId}&identity_provider=${provider}&scope=${oauthConfig.scopes.join(' ')}&state=${oauthConfig.state}&code_challenge=${oauthConfig.codeChallenge}&code_challenge_method=S256`
    var dataLayer = window.dataLayer || [];
    switch (provider) {
        case "Facebook":
            dataLayer.push({
                'event': 'pt_sign_in',
                'eventAction':'sign_in',
                'eventLabel': 'FB',
                'eventParam1': 'FB',
                'click_url': url
            })
            break;
        case "Twitter":
            dataLayer.push({
                'event': 'pt_sign_in',
                'eventAction': 'sign_in',
                'eventLabel': 'Twitter',
                'eventParam1': 'Twitter',
                'click_url': url
            })
            break;
        case "Google":
            dataLayer.push({
                'event': 'pt_sign_in',
                'eventAction': 'sign_in',
                'eventLabel': 'Google',
                'eventParam1': 'Google',
                'click_url': url
            })
            break;
        case "WeChat":
            dataLayer.push({
                'event': 'pt_sign_in',
                'eventAction': 'sign_in',
                'eventLabel': 'Wechat',
                'eventParam1': 'Wechat',
                'click_url': url
            })
            break;
        case "SignInWithApple":
            dataLayer.push({
                'event': 'pt_sign_in',
                'eventAction': 'sign_in',
                'eventLabel': 'SignInWithApple',
                'eventParam1': 'SignInWithApple',
                'click_url': url
            })
            break;
    }
    location.href = url
}

function openLoader() {
    document.getElementsByClassName("loader-wrapper")[0].style.display = "flex"
}

function closeLoader() {
    document.getElementsByClassName("loader-wrapper")[0].style.display = "none"
}

function getCookie(cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

String.prototype.toMMSS = function () {
    var sec_num = parseInt(this, 10);
    var minutes = Math.floor(sec_num / 60);
    var seconds = sec_num - (minutes * 60);

    if (minutes < 10) { minutes = "0" + minutes; }
    if (seconds < 10) { seconds = "0" + seconds; }
    return minutes + ':' + seconds;
}

function dynamicAddCss(attr, url)
{
    $(attr).insertAfter(url);
}