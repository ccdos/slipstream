﻿using System;
using System.Collections.Generic;
using System.Text;

using Malt.Layout.Models;

namespace Malt.Layout
{
    public interface IWidgetFactory
    {
        /// <summary>
        /// 创建自定义控件
        /// </summary>
        object CreateFieldWidget(Field field);

        /// <summary>
        /// 创建表格布局控件
        /// </summary>
        /// <returns></returns>
        ITableLayoutWidget CreateTableLayoutWidget();

        /// <summary>
        /// 创建标签控件
        /// </summary>
        /// <returns></returns>
        object CreateLabelWidget(Models.Label label);

        object CreateNotebookWidget(Models.Notebook notebook);

        object CreatePageWidget(Models.Page page, object parentWidget, object childContent);

        /// <summary>
        /// 创建水平线控件
        /// </summary>
        /// <returns></returns>
        object CreateHorizontalLineWidget(Models.HorizontalLine hl);
    }
}
