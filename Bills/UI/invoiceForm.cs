﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bills.DBcontext;

namespace Bills.UI
{
    public partial class invoiceForm : Form
    {
        BillSystemDB db;
        List<int> ItemsQuantity;
        BindingList<Item> ItemsObjects;

        public invoiceForm()
        {
            InitializeComponent();
            db = new BillSystemDB();
            ItemsQuantity = new List<int>();
            ItemsObjects = new BindingList<Item>();
            dtp_invoiceTime.Format = DateTimePickerFormat.Time;
            dtp_invoiceTime.ShowUpDown = true;
            lbl_invoiceSaveStatus.Text = "editing......";
        }

        private void invoiceForm_Load(object sender, EventArgs e)
        {
            //var invoiceNextId = db.Sales_Invoices.SqlQuery("select IDENT_CURRENT('Sales_Invoices') AS Current_Identity").SingleOrDefault();
            //txt_invoiceNumber.Text = invoiceNextId.invoice_id.ToString(); 
            try
            {
                var invoiceNextId = db.Sales_Invoices.Max(invoice => invoice.invoice_id);
                txt_invoiceNumber.Text = Convert.ToString(invoiceNextId + 1);

            }
            catch (Exception ex)
            {
                int invoiceNextId = 1;
                txt_invoiceNumber.Text = Convert.ToString(invoiceNextId);

            }

            cb_invoiceClient.DataSource = db.Clients.ToList();
            cb_invoiceClient.DisplayMember = "client_name";
            cb_invoiceClient.ValueMember = "client_id";

            cb_ivoiceItemName.DataSource = db.Items.ToList();
            cb_ivoiceItemName.DisplayMember = "item_name";
            cb_ivoiceItemName.ValueMember = "item_Code";

            cb_invoiceEmployee.DataSource = db.Employees.ToList();
            cb_invoiceEmployee.DisplayMember = "employee_name";
            cb_invoiceEmployee.ValueMember = "employee_id";

            nud_invoiceItemQuantity.Enabled = false;
        }

        private void btn_compSave_Click(object sender, EventArgs e)
        {
            string messageError = "";
            bool error = false;
            if(ItemsObjects.Count == 0)
            {
                messageError += "One item at least must be added";
                error = true;
            }
            if(nud_billPaidUp.Value < Decimal.Parse(txt_billNet.Text))
            {
                messageError += "\nPaid Up must be greater than Bill Net";
                error = true;
            }
            if (!error)
            {
                DateTime dt = DateTime.Now;
                Sales_Invoice invoice = new Sales_Invoice()
                {
                    invoice_time = dt.Subtract(dtp_invoiceTime.Value),
                    invoice_date = dtp_incoiceDate.Value,
                    invoice_percentage_discount = (float)nud_percentageDiscount.Value,
                    invoice_employee_id = (int)cb_invoiceEmployee.SelectedValue,
                    invoice_client_id = (int)cb_invoiceClient.SelectedValue
                };
                db.Sales_Invoices.Add(invoice);
                db.SaveChanges();
                int invoiceId = db.Sales_Invoices.Max(i => i.invoice_id);
                for (int i = 0; i < ItemsObjects.Count; i++)
                {
                    db.Invoive_Items.Add(new Invoive_items()
                    {
                        invoice_id = invoiceId,
                        item_id = ItemsObjects[i].item_Code,
                        quantity = ItemsQuantity[i]
                    });
                }
                db.SaveChanges();
                lbl_invoiceSaveStatus.Text = "Done";
                lbl_invoiceSaveStatus.ForeColor = Color.Green;
                btn_compCancle_Click(null, null);
            }
            else
            {
                lbl_invoiceSaveStatus.Text = messageError;
                lbl_invoiceSaveStatus.ForeColor = Color.Red;
            }
        }

        private void btn_compCancle_Click(object sender, EventArgs e)
        {
            dtp_incoiceDate.Value = dtp_invoiceTime.Value = DateTime.Now;
            txt_billNet.Text = txt_invoiceSellPrice.Text = txt_invoiceItemTotal.Text = "";
            txt_invoiceTotal.Text = txt_rest.Text = txt_valueDiscount.Text = "";
            nud_billPaidUp.Value = nud_invoiceItemQuantity.Value = nud_percentageDiscount.Value = 0;
        }

        private void updateGridView()
        {
            //dgv_invoiceItems.Update();
            dgv_invoiceItems.DataSource = ItemsObjects.Select(it => new {it.item_Code,it.item_name,
                Quantity = nud_invoiceItemQuantity.Value,
                it.sell_price,Total = txt_invoiceItemTotal.Text,totalBalance = txt_invoiceTotal.Text}).ToList();
                
            }

        private void btn_invoiceAddItem_Click(object sender, EventArgs e)
        {
            if (txt_invoiceSellPrice.Text == "")
            {
                lbl_addStatus.Text = "Choose item first";
                lbl_addStatus.ForeColor = Color.Red;
            }
            else if((int)nud_invoiceItemQuantity.Value != 0)
            {
                ItemsQuantity.Add((int)nud_invoiceItemQuantity.Value);
                var item = db.Items.SingleOrDefault(i => i.item_Code == (int)cb_ivoiceItemName.SelectedValue);
                ItemsObjects.Add(item);
                calulateTotalBill();
                nud_percentageDiscount_ValueChanged(null, null);
                updateGridView();
                nud_invoiceItemQuantity.Value = 0;
                txt_invoiceItemTotal.Text = "";
                txt_invoiceSellPrice.Text = "";
                lbl_addStatus.Text = "Done";
                lbl_addStatus.ForeColor = Color.Green;
                nud_invoiceItemQuantity.Enabled = false;
            }
            else
            {
                lbl_addStatus.Text = "Quantity Must be Greater than Zero";
                lbl_addStatus.ForeColor = Color.Red;
            }

        }
        private void calulateTotalBill()
        {
            decimal sum = 0;
            for(int i = 0; i < ItemsObjects.Count; i++)
            {
                 sum += ItemsQuantity[i] * ItemsObjects[i].sell_price;
            }
            txt_invoiceTotal.Text = sum.ToString();
        }
        private void cb_ivoiceItemName_SelectionChangeCommitted(object sender, EventArgs e)
        {
            var item = db.Items.SingleOrDefault(i => i.item_Code == (int)cb_ivoiceItemName.SelectedValue) ;
            //MessageBox.Show($"{item}");
            txt_invoiceSellPrice.Text = ((decimal)item.sell_price).ToString();
            nud_invoiceItemQuantity.Enabled = true;
        }

        private void nud_invoiceItemQuantity_ValueChanged(object sender, EventArgs e)
        {
            txt_invoiceItemTotal.Text = (nud_invoiceItemQuantity.Value*Decimal.Parse(txt_invoiceSellPrice.Text)).ToString();
            nud_percentageDiscount.Value = 0;
            
        }

        private void cb_ivoiceItemName_MouseDown(object sender, MouseEventArgs e)
        {
            lbl_addStatus.Text = "";
        }

        private void nud_percentageDiscount_ValueChanged(object sender, EventArgs e)
        {
            txt_valueDiscount.Text = (nud_percentageDiscount.Value * decimal.Parse(txt_invoiceTotal.Text)/100).ToString();
            txt_billNet.Text = (Decimal.Parse(txt_invoiceTotal.Text) - Decimal.Parse(txt_valueDiscount.Text)).ToString();
        }

        private void nud_billPaidUp_ValueChanged(object sender, EventArgs e)
        {
            txt_rest.Text = (nud_billPaidUp.Value - Decimal.Parse(txt_billNet.Text)).ToString();
        }

    }
}
