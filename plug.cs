using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;

using CamBam;
using CamBam.UI;
using CamBam.CAD;
using CamBam.Util;

namespace Matmill
{
    // Insert logger into the matmill namespace to be bound in compile-time
    class Logger
    {
        static public void log(int level, string s, params object[] args)
        {
            ThisApplication.AddLogMessage(level, s, args);
        }
        static public void log(string s, params object[] args)
        {
            ThisApplication.AddLogMessage(4, s, args);
        }
        static public void warn(string s, params object[] args)
        {
            ThisApplication.AddLogMessage("Trocho warning: " + s, args);
        }
        static public void err(string s, params object[] args)
        {
            ThisApplication.AddLogMessage("Trocho error: " + s, args);
        }
    }
}

namespace Trochomops
{
    // alias for compatibility with the old name of trochoidal pocket
    [Serializable]
    public class Mop_matmill : MOPTrochopock
    {

    }

    public static class Plug
    {
        const string pocket_mop_name = "Trochoidal Pocket";
        const string profile_mop_name = "Trochoidal Profile";

        private static void pocket_mop_onclick(object sender, EventArgs ars)
        {
            if (!PolylineUtils.ConfirmSelected(CamBamUI.MainUI.ActiveView))
            {
                return;
            }

            MOPTrochopock mop = new MOPTrochopock(CamBamUI.MainUI.ActiveView.CADFile, CamBamUI.MainUI.ActiveView.Selection);
            CamBamUI.MainUI.InsertMOP(mop);
        }

        private static void profile_mop_onclick(object sender, EventArgs ars)
        {
            if (!PolylineUtils.ConfirmSelected(CamBamUI.MainUI.ActiveView))
            {
                return;
            }

            MOPTrochoprof mop = new MOPTrochoprof(CamBamUI.MainUI.ActiveView.CADFile, CamBamUI.MainUI.ActiveView.Selection);
            CamBamUI.MainUI.InsertMOP(mop);
        }

        private static void insert_in_top_menu(CamBamUI ui, ToolStripMenuItem entry)
        {
            for (int i = 0; i < ui.Menus.mnuMachining.DropDownItems.Count; ++i)
            {
                ToolStripItem tsi = ui.Menus.mnuMachining.DropDownItems[i];
                if (tsi is ToolStripSeparator || i == ui.Menus.mnuMachining.DropDownItems.Count - 1)
                {
                    ui.Menus.mnuMachining.DropDownItems.Insert(i, entry);
                    return;
                }
            }
        }

        private static void insert_in_context_menu(CamBamUI ui, ToolStripMenuItem entry)
        {
            foreach (ToolStripItem tsi in ui.ViewContextMenus.ViewContextMenu.Items)
            {
                if (tsi is ToolStripMenuItem && tsi.Name == "machineToolStripMenuItem")
                {
                    ToolStripMenuItem tsmi = (ToolStripMenuItem)tsi;
                    for (int i = 0; i < tsmi.DropDownItems.Count; ++i)
                    {
                        if (tsmi.DropDownItems[i] is ToolStripSeparator || i == tsmi.DropDownItems.Count - 1)
                        {
                            tsmi.DropDownItems.Insert(i, entry);
                            return;
                        }
                    }
                }
            }
        }

        private static void insert_in_toolbar(ToolStripButton button)
        {
            foreach (Control c in ThisApplication.TopWindow.Controls)
            {
                if (c is ToolStripContainer)
                {
                    foreach (Control cc in ((ToolStripContainer)c).TopToolStripPanel.Controls)
                    {
                        if (cc is CAMToolStrip)
                        {
                            CAMToolStrip strip = (CAMToolStrip)cc;

                            // check if 'Custom CAMToolbar plugin' already iserted us
                            foreach (ToolStripButton b in strip.Items)
                            {
                                if (b.ToolTipText == button.ToolTipText)
                                    return;
                            }
                            strip.Items.Add(button);
                            return;
                        }
                    }
                }
            }
        }

        private static void on_window_shown(object sender, EventArgs e)
        {
            ThisApplication.TopWindow.Shown -= on_window_shown;

            ToolStripButton button;

            button = new ToolStripButton();
            button.ToolTipText = TextTranslation.Translate(profile_mop_name);
            button.Click += profile_mop_onclick;
            button.Image = resources.cam_trochoprof1;

            insert_in_toolbar(button);

            button = new ToolStripButton();
            button.ToolTipText = TextTranslation.Translate(pocket_mop_name);
            button.Click += pocket_mop_onclick;
            button.Image = resources.cam_trochopock1;

            insert_in_toolbar(button);
        }

        public static void InitPlugin(CamBamUI ui)
        {
            ToolStripMenuItem menu_entry;

            menu_entry = new ToolStripMenuItem();
            menu_entry.Text = profile_mop_name;
            menu_entry.Click += profile_mop_onclick;
            menu_entry.Image = resources.cam_trochoprof1;

            insert_in_top_menu(ui, menu_entry);

            menu_entry = new ToolStripMenuItem();
            menu_entry.Text = profile_mop_name;
            menu_entry.Click += profile_mop_onclick;
            menu_entry.Image = resources.cam_trochoprof1;

            insert_in_context_menu(ui, menu_entry);

            menu_entry = new ToolStripMenuItem();
            menu_entry.Text = pocket_mop_name;
            menu_entry.Click += pocket_mop_onclick;
            menu_entry.Image = resources.cam_trochopock1;

            insert_in_top_menu(ui, menu_entry);

            menu_entry = new ToolStripMenuItem();
            menu_entry.Text = pocket_mop_name;
            menu_entry.Click += pocket_mop_onclick;
            menu_entry.Image = resources.cam_trochopock1;

            insert_in_context_menu(ui, menu_entry);

            // defer attachment to toolbar until the first show.
            // Custom CAM Toolbar plugin (if installed) may already attached us after Load event, so we react on later Shown event
            ThisApplication.TopWindow.Shown += on_window_shown;

            if (CADFile.ExtraTypes == null)
                CADFile.ExtraTypes = new List<Type>();
            CADFile.ExtraTypes.Add(typeof(MOPTrochopock));
            CADFile.ExtraTypes.Add(typeof(Mop_matmill));
            CADFile.ExtraTypes.Add(typeof(MOPTrochoprof));

            {
                MOPTrochopock o = new MOPTrochopock();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(MOPTrochopock));
                MemoryStream stream = new MemoryStream();
                xmlSerializer.Serialize(stream, o);
            }

            {
                MOPTrochoprof o = new MOPTrochoprof();
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(MOPTrochoprof));
                MemoryStream stream = new MemoryStream();
                xmlSerializer.Serialize(stream, o);
            }
        }
    }
}