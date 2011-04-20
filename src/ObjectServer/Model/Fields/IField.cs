﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.Model
{
    public interface IField : IFieldDescriptor
    {
        FieldValueGetter Getter { get; set; }
        FieldDefaultValueGetter DefaultProc { get; set; }

        void Validate();

        Dictionary<long, object> GetFieldValues(
            IServiceScope scope, ICollection<Dictionary<string, object>> records);

        object SetFieldValue(IServiceScope scope, object value);

        object BrowseField(IServiceScope scope, IDictionary<string, object> record);

        #region Fluent interface

        IField SetLabel(string label);
        IField Required();
        IField NotRequired();
        IField ValueGetter(FieldValueGetter fieldGetter);
        IField DefaultValueGetter(FieldDefaultValueGetter defaultProc);
        IField SetSize(int size);
        IField SetHelp(string help);
        IField Readonly();
        IField NotReadonly();
        IField OnDelete(OnDeleteAction act);
        IField BeProperty();
        IField SetOptions(IDictionary<string, string> options);

        #endregion

    }
}