﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    class BulletinBoardGump : Gump
    {
        //private HtmlControl _htmlControl;
        private ScrollArea _area;
        private Item _item;

        public BulletinBoardGump(Item item, int x, int y, string name) : base(item, 0)
        {
            _item = item;
            _item.Items.Added += ItemsOnAdded;
            _item.Items.Removed += ItemsOnRemoved;
            _item.Disposed += ItemOnDisposed;

            X = x;
            Y = y;
            CanMove = true;
            CanCloseWithRightClick = true;


            Add(new GumpPic(0, 0, 0x087A, 0));

            Label label = new Label(name, false, 0x0386, 170, 2, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 159, Y = 36
            };
            
            Add(label);

            HitBox hitbox = new HitBox(15, 170, 80, 80)
            {
                IsTransparent = true,
                Alpha = 1
            };
            hitbox.MouseClick += (sender, e) =>
            {
                Engine.UI.GetByLocalSerial<BulletinBoardItem>(LocalSerial)?.Dispose();

                Engine.UI.Add(new BulletinBoardItem(LocalSerial, 0, World.Player.Name, string.Empty, "Date/Time", string.Empty, 0));
            };
            Add(hitbox);

            _area = new ScrollArea(127, 162, 241, 155, false);
            Add(_area);
        }

        private void ItemOnDisposed(object sender, EventArgs e)
        {
            Dispose();
        }

        private void ItemsOnRemoved(object sender, CollectionChangedEventArgs<Item> e)
        {
            foreach (BulletinBoardObject v in Children.OfType<BulletinBoardObject>().Where(s => e.Contains(s.Item)))
                v.Dispose();
        }

        private void ItemsOnAdded(object sender, CollectionChangedEventArgs<Item> e)
        {
            foreach (BulletinBoardObject v in Children.OfType<BulletinBoardObject>().Where(s => e.Contains(s.Item)))
                v.Dispose();

            foreach (Item item in e)
            {
                NetClient.Socket.Send(new PBulletinBoardRequestMessageSummary(LocalSerial, item));
            }
        }


        public void Add(BulletinBoardObject obj)
        {
            _area.Add(obj);
        }

        public override void Dispose()
        {

            if (_item != null)
            {
                _item.Items.Added -= ItemsOnAdded;
                _item.Items.Removed -= ItemsOnRemoved;
                _item.Disposed -= ItemOnDisposed;
            }

            base.Dispose();
        }
    }


    class BulletinBoardItem : Gump
    {
        private ScrollFlag _scrollBar;
        private MultiLineBox _textBox;
        private TextBox _subjectTextbox;
        private Button _buttonPost, _buttonReply, _buttonRemove;

        private Serial _msgSerial;

        public BulletinBoardItem(Serial serial, Serial msgSerial, string poster, string subject, string datatime, string data, byte variant) : base(serial, 0)
        {
            _msgSerial = msgSerial;
            AcceptKeyboardInput = true;
            CanMove = true;
            CanCloseWithRightClick = true;

            Add(new ExpandableScroll(0, 0, 250)
            {
                TitleGumpID = 0x0820,
            });
            _scrollBar = new ScrollFlag( 0, 0, Height);
            Add(_scrollBar);
            bool useUnicode = FileManager.ClientVersion >= ClientVersions.CV_305D;
            byte unicodeFontIndex = 1;
            int unicodeFontHeightOffset = 0;

            ushort textColor = 0x0386;

            if (useUnicode)
            {
                unicodeFontHeightOffset = -6;
                textColor = 0;
            }

            Label text = new Label("Author:", useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)6)
            {
                X = 30,
                Y = 40
            };
            Add(text);

            text = new Label(poster, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)9)
            {
                X = 30 + text.Width, Y = 46 + unicodeFontHeightOffset
            };
            Add(text);



            text = new Label("Time:", useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)6)
            {
                X = 30,
                Y = 56
            };
            Add(text);

            text = new Label(datatime, useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)9)
            {
                X = 30 + text.Width,
                Y = 62 + unicodeFontHeightOffset
            };
            Add(text);


            text = new Label("Subject:", useUnicode, textColor, font: useUnicode ? unicodeFontIndex : (byte)6)
            {
                X = 30,
                Y = 72
            };
            Add(text);

          
            ushort subjectColor = textColor;

            if (variant == 0)
                subjectColor = 0x0008;

            //HitBox hitBox = new HitBox(30 + text.Width, 78, 160, 16)
            //{
            //    IsTransparent = true,
            //    Alpha = 1
            //};
            //AddChildren(hitBox);


            Add(_subjectTextbox = new TextBox(useUnicode ? unicodeFontIndex : (byte)9, maxWidth: 150, width: 150, isunicode: useUnicode, hue: subjectColor)
            {
                X = 30 + text.Width,
                Y = 78 + unicodeFontHeightOffset,
                Width = 150
            });
            _subjectTextbox.SetText(subject);

            
            Add(new GumpPicTiled(30 ,100, 204, 4, 0x0835));

            Add(_textBox = new MultiLineBox(new MultiLineEntry(useUnicode ? unicodeFontIndex : (byte)9, width: 220, maxWidth: 220, hue: textColor, unicode: useUnicode), true)
            {
                X = 40,
                Y = 120,
                Width = 220,
                ScissorsEnabled = true,
                Text = data
            });

            switch (variant)
            {
                case 0:
                    Add(new GumpPic(97, 12, 0x0883, 0));
                    Add(_buttonPost = new Button((int)ButtonType.Post, 0x0886, 0x0886)
                    {
                        X = 37, Y = Height - 50,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true,
                    });
                    break;
                case 1:
                    Add(_buttonReply = new Button((int)ButtonType.Reply, 0x0884, 0x0884)
                    {
                        X = 37,
                        Y = Height - 50,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true,
                    });
                    break;
                case 2:
                    Add(_buttonRemove = new Button((int)ButtonType.Remove, 0x0885, 0x0885)
                    {
                        X = 235,
                        Y = Height - 50,
                        ButtonAction = ButtonAction.Activate,
                        ContainsByBounds = true,
                    });
                    break;
            }
        }


        enum ButtonType
        {
            Post,
            Remove,
            Reply
        }


        public override void OnButtonClick(int buttonID)
        {
            // necessary to avoid closing
            if (_subjectTextbox == null)
                return;

            switch ((ButtonType) buttonID)
            {
                case ButtonType.Post:
                    NetClient.Socket.Send(new PBulletinBoardPostMessage(LocalSerial, 0, _subjectTextbox.Text, _textBox.Text));
                    Dispose();
                    break;
                case ButtonType.Remove:
                    Engine.UI.Add(new BulletinBoardItem(LocalSerial, 0, World.Player.Name, "RE: " + _subjectTextbox.Text, "Date/Time", string.Empty, 0));
                    Dispose();
                    break;
                case ButtonType.Reply:
                    NetClient.Socket.Send(new PBulletinBoardRemoveMessage(LocalSerial, _msgSerial));
                    Dispose();
                    break;
            }
        }

        protected override void OnMouseWheel(MouseEvent delta)
        {
            switch (delta)
            {
                case MouseEvent.WheelScrollUp:
                    _scrollBar.Value -= 5;

                    break;
                case MouseEvent.WheelScrollDown:
                    _scrollBar.Value += 5;

                    break;
            }
        }

        public override void OnKeyboardReturn(int textID, string text)
        {
            if ((MultiLineBox.PasteRetnCmdID & textID) != 0 && !string.IsNullOrEmpty(text))
            {
                _textBox.TxEntry.InsertString(text.Replace("\r", string.Empty));
            }
        }

        public override void Update(double totalMS, double frameMS)
        {
            WantUpdateSize = true;
            _textBox.Height = Height - 185;

            if (_buttonPost != null)
                _buttonPost.Y = Height - 50;
            if (_buttonReply != null)
                _buttonReply.Y = Height - 50;
            if (_buttonRemove != null)
                _buttonRemove.Y = Height - 50;

            base.Update(totalMS, frameMS);
        }
    }

    class BulletinBoardObject : ScrollAreaItem
    {
        public BulletinBoardObject(Serial parent, Item serial, string text)
        {
            LocalSerial = parent;
            Item = serial;
            CanMove = false;
            bool unicode = FileManager.ClientVersion >= ClientVersions.CV_305D;

            Add(new GumpPic(0, 0, 0x1523, 0));
            Add(new Label(text, unicode, (ushort)(unicode ? 0 : 0x0386), font: (byte)(unicode ? 1 : 9)) { X = Children[Children.Count - 1].Texture.Width + 2 });
        }

        public Item Item { get; }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            NetClient.Socket.Send(new PBulletinBoardRequestMessage(LocalSerial, Item));

            return true;
        }
    }
}