import os
import re
import json

from sre_compile import isstring

SDK_ISSUES_SUPPORT_LANG_LIST = [
    'ar',
    'bn',
    'en',
    'es',
    'fr',
    'id',
    'it',
    'iw',
    'ja',
    'km',
    'lo',
    'my',
    'nl',
    'pl',
    'pt',
    'ro',
    'ru',
    'th',
    'tr',
    'vi',
    'zh-rCN',
    'zh-TW'
]

def convertToXmlString(sourceStr):
    if isstring(sourceStr) == False:
        return sourceStr
    sourceStr = sourceStr.replace("&", "&amp;")
    sourceStr = sourceStr.replace("<", "&lt;")
    sourceStr = sourceStr.replace(">", "&gt;")
    sourceStr = sourceStr.replace("\'", "&apos;")
    sourceStr = sourceStr.replace("\"", "&quot;")
    return sourceStr

def scanExistsIssueTranslation(dir = os.path.join(os.getcwd(), "translation\\")):
    language_map = {}

    for file_name in os.listdir(dir):
        full_path = os.path.join(dir, file_name)
        if os.path.isfile(full_path):
            lang = re.findall(r"issues_(.*).json", full_path)
            if (len(lang) > 0):
                language_map[lang[0]] = full_path
        elif os.path.isdir(full_path):
            # 继续遍历
            pass

    return language_map

def loadLocalIssuesData(file = os.path.join(os.getcwd(), "translation\\issues_cn.json")):
    issues_json = {}
    with open(file, 'r', encoding='utf-8') as file:
        issues_json = json.load(file)

    return issues_json

def wiriteDataToFile(file_path="", json_data={}):
    path = os.path.dirname(file_path)
    if not os.path.exists(path):
        os.makedirs(path)
        print("创建目录 {} 成功".format(path))

    # write to file
    json_str = json.dumps(json_data, indent=4, ensure_ascii=False)
    fd = open(file_path, 'w', encoding='utf-8')
    fd.write(json_str)
    fd.close()

def IsEmptyString(str: object = ""):
    if None == str:
        return True
    elif 0 == len(str):
        return True
    elif True == str.isspace():
        return True

    return False

def clearScreen():
    # 判断系统类型
    if os.name == 'nt':  # Windows系统
        os.system('cls')
    else:  # Mac和Linux系统
        os.system('clear')

def getIssuesFileByLang(lang : str):
    lang_str = 'cn'
    file_path = ''
    if lang == 'ar':
        lang_str = 'ar'
    elif lang == 'bn':
        lang_str = 'bn'
    elif lang == 'en':
        lang_str = 'en'
    elif lang == 'es':
        lang_str = 'es'
    elif lang == 'fr':
        lang_str = 'fr'
    elif lang == 'id':
        lang_str = 'id'
    elif lang == 'it':
        lang_str = 'it'
    elif lang == 'iw':
        lang_str = 'iw'
    elif lang == 'ja':
        lang_str = 'ja'
    elif lang == 'km':
        lang_str = 'km'
    elif lang == 'lo':
        lang_str = 'lo'
    elif lang == 'my':
        lang_str = 'my'
    elif lang == 'nl':
        lang_str = 'nl'
    elif lang == 'pl':
        lang_str = 'pl'
    elif lang == 'pt':
        lang_str = 'pt'
    elif lang == 'ro':
        lang_str = 'ro'
    elif lang == 'ru':
        lang_str = 'ru'
    elif lang == 'th':
        lang_str = 'th'
    elif lang == 'tr':
        lang_str = 'tr'
    elif lang == 'vi':
        lang_str = 'vi'
    elif lang == 'zh-rCN':
        lang_str = 'cn'
    elif lang == 'zh-TW':
        lang_str = 'tw'
    else:
        lang_str = lang

    file_path = os.path.join(os.getcwd(), ("issues_" + lang_str + ".json"))
    return lang_str, file_path
