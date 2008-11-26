﻿using System;
using Castle.ActiveRecord;
using NHibernate.Expression;
using System.Web;
using System.Collections.Generic;

namespace Entities
{
    [ActiveRecord(Table = "QuizItems")]
    public class QuizItem : ActiveRecordBase<QuizItem>
    {
        private int _id;
        private DateTime _created;
        private string _header;
        private string _body;
        private Operator _createdBy;
        private QuizItem _parent;
        private string _url;

        [PrimaryKey]
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        [Property(Unique=true)]
        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }

        [BelongsTo("FK_CreatedBy")]
        public Operator CreatedBy
        {
            get { return _createdBy; }
            set { _createdBy = value; }
        }

        [BelongsTo("FK_Parent")]
        public QuizItem Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        [Property]
        public DateTime Created
        {
            get { return _created; }
            set { _created = value; }
        }

        [Property(Length=150)]
        public string Header
        {
            get { return _header; }
            set { _header = value; }
        }

        [Property(ColumnType = "StringClob", SqlType = "TEXT")]
        public string Body
        {
            get { return _body; }
            set { _body = value; }
        }

        public int Score
        {
            get { return GetScore(); }
        }

        public int AnswersCount
        {
            get
            {
                return QuizItem.Count(Expression.Eq("Parent", this));
            }
        }

        public static IEnumerable<QuizItem> GetQuestions(Operator oper)
        {
            if (oper == null)
                return GetNewQuestions();
            else
                return GetQuestionsForUser(oper);
        }

        public static IEnumerable<QuizItem> GetNewQuestions()
        {
            return QuizItem.SlicedFindAll(0, 50, 
                new Order[] { Order.Desc("Created") },
                Expression.IsNull("Parent"));
        }

        private static IEnumerable<QuizItem> GetQuestionsForUser(Operator oper)
        {
            return QuizItem.FindAll(
                new Order[] { Order.Desc("Created") }, 
                Expression.Eq("CreatedBy", oper), 
                Expression.IsNull("Parent"));
        }

        public override void Save()
        {
            // Checking to see if this is FIRST saving and if it is create a new friendly URL...
            if (_id == 0)
            {
                Created = DateTime.Now;
                
                // Building UNIQUE friendly URL
                Url = Header.ToLower();
                if (Url.Length > 100)
                    Url = Url.Substring(0, 100);
                int index = 0;
                while (index < Url.Length)
                {
                    if (("abcdefghijklmnopqrstuvwxyz0123456789").IndexOf(Url[index]) == -1)
                    {
                        Url = Url.Substring(0, index) + "-" + Url.Substring(index + 1);
                    }
                    index += 1;
                }
                Url = Url.Trim('-');
                bool found = true;
                while (found)
                {
                    found = false;
                    if (Url.IndexOf("--") != -1)
                    {
                        Url = Url.Replace("--", "-");
                        found = true;
                    }
                }
                int countOfOldWithSameURL = QuizItem.Count(Expression.Like("Url", Url + "%.quiz"));
                if (countOfOldWithSameURL > 0)
                    Url += (countOfOldWithSameURL + 1).ToString();
                Url += ".quiz";
                base.Save();
            }
            base.Save();
        }

        public int GetScore()
        {
            int plus = Vote.Count(Expression.Eq("QuizItem", this), Expression.Eq("Score", 1));
            int minus = Vote.Count(Expression.Eq("QuizItem", this), Expression.Eq("Score", -1));
            return plus - minus;
        }

        public IEnumerable<QuizItem> GetAnswers()
        {
            return QuizItem.FindAll(Expression.Eq("Parent", this));
        }

        public int CountFavorites(Operator exclude)
        {
            if (exclude == null)
            {
                return Favorite.Count(Expression.Eq("Question", this));
            }
            else
            {
                return Favorite.Count(Expression.Eq("Question", this), Expression.Not(Expression.Eq("FavoredBy", exclude)));
            }
        }
    }
}