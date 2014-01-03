// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using RegressionTests.Shared;
using hMailServer;
using Attachment = System.Net.Mail.Attachment;

namespace RegressionTests.POP3
{
   [TestFixture]
   public class Basics : TestFixtureBase
   {
      private string[] GetTestFiles()
      {
         var files = new List<string>();
         Assembly a = Assembly.GetExecutingAssembly();
         files.Add(a.Location);

         // create a file with a lot of dots.
         var sb = new StringBuilder();
         for (int i = 0; i < 10000; i++)
         {
            sb.Append("....................");
         }

         string tempFile = Path.GetTempFileName() + ".txt";
         File.WriteAllText(tempFile, sb.ToString());
         files.Add(tempFile);

         return files.ToArray();
      }

      [Test]
      [Description("Test to send a number of attachments...")]
      public void TestAttachmentEncoding()
      {
         string[] testFiles = GetTestFiles();

         SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         foreach (string testFile in testFiles)
         {
            Trace.WriteLine(testFile);

            string fileHash = TestSetup.GetFileHash(testFile);

            var mail = new MailMessage();
            mail.From = new MailAddress("test@test.com");
            mail.To.Add("test@test.com");
            mail.Subject = "Test";
            mail.Attachments.Add(new Attachment(testFile));

            TestSetup.SendMessage(mail);

            POP3Simulator.AssertMessageCount("test@test.com", "test", 1);

            var sim = new POP3Simulator();
            sim.ConnectAndLogon("test@test.com", "test");
            string fileContent = sim.RETR(1);
            sim.DELE(1);
            sim.QUIT();


            var message = new Message();

            try
            {
               File.WriteAllText(message.Filename, fileContent);
               message.RefreshContent();

               message.Attachments[0].SaveAs(message.Filename);
               string fileHashAfter = TestSetup.GetFileHash(message.Filename);

               Assert.AreEqual(fileHash, fileHashAfter);
            }
            finally
            {
               File.Delete(message.Filename);
            }
         }
      }

      [Test]
      public void TestConnection()
      {
         // Create a test account
         // Fetch the default domain
         SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "pop3user@test.com", "test");

         // Send 5 messages to this account.
         var oSMTP = new SMTPClientSimulator();
         for (int i = 0; i < 5; i++)
            oSMTP.Send("test@test.com", "pop3user@test.com", "INBOX", "POP3 test message");


         POP3Simulator.AssertMessageCount("pop3user@test.com", "test", 5);
      }

      [Test]
      public void TestDELEInvalid()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody" + i.ToString());

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         Assert.IsFalse(sim.DELE(0));
         Assert.IsFalse(sim.DELE(-1));
         Assert.IsFalse(sim.DELE(1000));
         Assert.IsTrue(sim.DELE(5));
      }


      [Test]
      public void TestLIST()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody1");
         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody2");
         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody3");

         POP3Simulator.AssertMessageCount(account.Address, "test", 3);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.LIST();

         Assert.IsTrue(result.Contains("3 messages"));
         Assert.IsTrue(result.Contains("\r\n1"));
         Assert.IsTrue(result.Contains("\r\n2"));
         Assert.IsTrue(result.Contains("\r\n3"));
         Assert.IsTrue(result.Contains("\r\n."));
      }

      [Test]
      public void TestLISTInvalid()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody" + i.ToString());

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.LIST(0);
         Assert.IsTrue(result.Contains("No such message"));
         result = sim.LIST(-1);
         Assert.IsTrue(result.Contains("No such message"));
         result = sim.LIST(100);
         Assert.IsTrue(result.Contains("No such message"));
      }

      [Test]
      public void TestLISTSpecific()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody1");
         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody2");
         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody3");

         POP3Simulator.AssertMessageCount(account.Address, "test", 3);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.LIST(2);

         Assert.IsTrue(result.Contains("OK 2"));

         result = sim.LIST(3);
         Assert.IsTrue(result.Contains("OK 3"));
      }

      [Test]
      public void TestLISTWithDeleted()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody" + i.ToString());

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         sim.DELE(2);
         sim.DELE(4);
         string result = sim.LIST();

         Assert.IsTrue(result.Contains("8 messages"));
         Assert.IsTrue(result.Contains("\r\n1"));
         Assert.IsTrue(result.Contains("\r\n3"));
         Assert.IsTrue(result.Contains("\r\n5"));
         Assert.IsTrue(result.Contains("\r\n."));
      }

      [Test]
      [Description("Test to log on a mailbox containing a message which has been marked as deleted using IMAP")]
      public void TestLogonMailboxWithDeletedMessage()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 3; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test",
                                           "Line1\r\nLine2\r\nLine3\r\nLine4\r\nLine\r\n");

         // Mark the second message as deleted using IMAP.
         POP3Simulator.AssertMessageCount(account.Address, "test", 3);

         var sim = new IMAPSimulator();
         sim.ConnectAndLogon(account.Address, "test");
         sim.SelectFolder("INBOX");
         sim.SetDeletedFlag(2);
         sim.Disconnect();

         // Now list messages and confirm that all are listed.

         var pop3Client = new POP3Simulator();
         pop3Client.ConnectAndLogon(account.Address, "test");
         string listResponse = pop3Client.LIST();
         string uidlResponse = pop3Client.UIDL();

         Assert.IsTrue(listResponse.Contains("\r\n1"));
         Assert.IsTrue(listResponse.Contains("\r\n2"));
         Assert.IsTrue(listResponse.Contains("\r\n3"));
         Assert.IsTrue(listResponse.Contains("\r\n.\r\n"));
         Assert.IsTrue(listResponse.Contains("3 messages"));

         Assert.IsTrue(uidlResponse.Contains("\r\n1"));
         Assert.IsTrue(uidlResponse.Contains("\r\n2"));
         Assert.IsTrue(uidlResponse.Contains("\r\n3"));
         Assert.IsTrue(uidlResponse.Contains("\r\n.\r\n"));
         Assert.IsTrue(uidlResponse.Contains("3 messages"));
      }

      [Test]
      public void TestPOP3TransactionSafety()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         Assert.IsTrue(SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody"));
         POP3Simulator.AssertMessageCount(account.Address, "test", 1);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");

         // Now delete the message using an IMAP client.
         var imapSimulator = new IMAPSimulator();
         Assert.IsTrue(imapSimulator.ConnectAndLogon(account.Address, "test"));
         Assert.IsTrue(imapSimulator.SelectFolder("INBOX"));
         Assert.IsTrue(imapSimulator.SetDeletedFlag(1));
         Assert.IsTrue(imapSimulator.Expunge());
         Assert.AreEqual(0, imapSimulator.GetMessageCount("Inbox"));

         Assert.IsTrue(SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody"));
         IMAPSimulator.AssertMessageCount(account.Address, "test", "Inbox", 1);

         // This deletion should not have any effect, since the POP3 connection is referencing an old message.
         sim.DELE(1);
         sim.QUIT();

         Assert.AreEqual(1, imapSimulator.GetMessageCount("Inbox"));
      }

      [Test]
      public void TestRETR()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody1");
         POP3Simulator.AssertMessageCount(account.Address, "test", 1);

         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody2");
         POP3Simulator.AssertMessageCount(account.Address, "test", 2);

         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody3");
         POP3Simulator.AssertMessageCount(account.Address, "test", 3);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.RETR(1);
         Assert.IsTrue(result.Contains("TestBody1"), result);
         result = sim.RETR(2);
         Assert.IsTrue(result.Contains("TestBody2"), result);
         result = sim.RETR(3);
         Assert.IsTrue(result.Contains("TestBody3"), result);

         Assert.IsFalse(result.Contains(".\r\n."));
      }


      [Test]
      public void TestTOPInvalid()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody" + i.ToString());

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         Assert.IsTrue(sim.TOP(-1, 0).Contains("No such message"));
         Assert.IsTrue(sim.TOP(0, 0).Contains("No such message"));
         Assert.IsTrue(sim.TOP(100, 0).Contains("No such message"));
      }

      [Test]
      public void TestTOPSpecificEntire()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody" + i.ToString());

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.TOP(1, 0);

         Assert.IsTrue(result.Contains("Received"));
         Assert.IsTrue(result.Contains("Subject"));
      }

      [Test]
      public void TestTOPSpecificPartial()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test",
                                           "Line1\r\nLine2\r\nLine3\r\nLine4\r\nLine\r\n");

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.TOP(4, 2);

         Assert.IsTrue(result.Contains("Received"));
         Assert.IsTrue(result.Contains("Line1"));
         Assert.IsTrue(result.Contains("Line2"));
         Assert.IsFalse(result.Contains("Line3"));
         Assert.IsFalse(result.Contains("Line4"));
      }

      [Test]
      public void TestUIDLInvalid()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody" + i.ToString());

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.UIDL(0);
         Assert.IsTrue(result.Contains("No such message"));
         result = sim.UIDL(-1);
         Assert.IsTrue(result.Contains("No such message"));
         result = sim.UIDL(100);
         Assert.IsTrue(result.Contains("No such message"));
      }

      [Test]
      public void TestUIDLSpecific()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody1");
         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody2");
         SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody3");

         POP3Simulator.AssertMessageCount(account.Address, "test", 3);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         string result = sim.UIDL(2);

         Assert.IsTrue(result.Contains("OK 2"));

         result = sim.UIDL(3);
         Assert.IsTrue(result.Contains("OK 3"));
      }

      [Test]
      public void TestUIDLWithDeleted()
      {
         Account account = SingletonProvider<TestSetup>.Instance.AddAccount(_domain, "test@test.com", "test");

         for (int i = 1; i <= 10; i++)
            SMTPClientSimulator.StaticSend(account.Address, account.Address, "Test", "TestBody" + i.ToString());

         POP3Simulator.AssertMessageCount(account.Address, "test", 10);

         var sim = new POP3Simulator();
         sim.ConnectAndLogon(account.Address, "test");
         sim.DELE(2);
         sim.DELE(4);
         string result = sim.UIDL();

         Assert.IsTrue(result.Contains("8 messages"));
         Assert.IsTrue(result.Contains("\r\n1"));
         Assert.IsTrue(result.Contains("\r\n3"));
         Assert.IsTrue(result.Contains("\r\n5"));
         Assert.IsTrue(result.Contains("\r\n."));
      }

      [Test]
      public void WelcomeMessage()
      {
         SingletonProvider<TestSetup>.Instance.GetApp().Settings.WelcomePOP3 = "HOWDYHO POP3";

         var oSimulator = new POP3Simulator();

         string sWelcomeMessage = oSimulator.GetWelcomeMessage();

         if (sWelcomeMessage != "+OK HOWDYHO POP3\r\n")
            throw new Exception("ERROR - Wrong welcome message.");
      }
   }
}