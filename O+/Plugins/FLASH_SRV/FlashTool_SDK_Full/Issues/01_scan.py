#!/usr/bin/python3

import json
import os

from openpyxl.workbook import Workbook

from pathDef import SDK_ISSUES_SUPPORT_LANG_LIST, getIssuesFileByLang
from pathDef import wiriteDataToFile

def scanLocalSdkIssuesLang():
    issues_json: object = {}
    not_exist_lang = []

    for index, str in enumerate(SDK_ISSUES_SUPPORT_LANG_LIST):
        lang, file_path = getIssuesFileByLang(str)
        print("index={}, lang={}, path={}\r\n".format(index, lang, file_path))

        # 先将缺少的语言文件保存到列表
        if not os.path.exists(file_path):
            not_exist_lang.append(lang)
            continue

        issues_item = {}
        with open(file_path, 'r', encoding='utf-8') as file:
            data = file.read()
            issues_item = json.loads(data)

        for item in issues_item:
            code = item["code"]
            text = item["text"]

            # 词条不存在
            if code not in issues_json:
                issues_json[code] = {}
                issues_json[code]["source"] = text
                issues_json[code]["translation"] = {}
                issues_json[code]["translation"][lang] = text
            else:
                issues_json[code]["translation"][lang] = text

    # 将缺失的词条增加到json数据中
    for key in issues_json:
        for lang in not_exist_lang:
            issues_json[key]["translation"][lang] = ""

    file_path = os.path.join(os.getcwd(), "allIssues.json")
    wiriteDataToFile(file_path, issues_json)

    return issues_json

def IssuesConvertExcel(issues_json:{}, file_path:str):
    # 创建excel工作簿
    book = Workbook()
    book.create_sheet("故障树错误码")
    excel_sheet = book["故障树错误码"]

    # 添加excel表头
    sheet_head = []
    sheet_head.append("code")
    for lang in SDK_ISSUES_SUPPORT_LANG_LIST:
        sheet_head.append(lang)
    excel_sheet.append(sheet_head)

    for code in issues_json:
        # 插入行数据到excel
        excel_data_row = []
        excel_data_row.append(code)
        for lang in SDK_ISSUES_SUPPORT_LANG_LIST:
            lang, path = getIssuesFileByLang(lang)
            excel_data_row.append(issues_json[code]["translation"][lang])
        excel_sheet.append(excel_data_row)

    # 保存excel数据
    book.save(file_path)

if __name__ == "__main__":
    issues_json = scanLocalSdkIssuesLang()
    file_path = os.path.join(os.getcwd(), "allIssues.xlsx")
    IssuesConvertExcel(issues_json, file_path)
    pass