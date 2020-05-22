using NodeApp.Converters;
using NodeApp.Interfaces;
using NodeApp.Interfaces.Services;
using NodeApp.MessengerData.DataTransferObjects;
using NodeApp.MessengerData.Services;
using ObjectsLibrary.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NodeApp.Tests
{
    public class PrivacyServiceTests
    {
        private readonly FillTestDbHelper fillTestDbHelper;
        private readonly IGroupsService gropsService;
        private readonly IContactsService contactsService;
        private readonly IPrivacyService privacyService;
        public PrivacyServiceTests()
        {
            TestsData testsData = TestsData.Create(nameof(PrivacyServiceTests));
            fillTestDbHelper = testsData.FillTestDbHelper;
            AppServiceProvider.Instance = testsData.AppServiceProvider;
            gropsService = testsData.AppServiceProvider.GroupsService;
            contactsService = testsData.AppServiceProvider.ContactsService;
            privacyService = testsData.AppServiceProvider.PrivacyService;
        }
        [Fact]
        public void UserApplyPrivacySettings()
        {
            var user = UserConverter.GetUserVm(fillTestDbHelper.Users.FirstOrDefault());            
            var hiddenUser = privacyService.ApplyPrivacySettings(user, user.Privacy);
            user.Privacy[1] = true;
            user.Privacy[2] = true;
            var publicUser = privacyService.ApplyPrivacySettings(user, user.Privacy);
            Assert.True(hiddenUser.About == null
                && hiddenUser.NameFirst == null
                && hiddenUser.NameSecond == null
                && hiddenUser.Tag == null
                && hiddenUser.Photo == null
                && hiddenUser.Emails == null
                && hiddenUser.Phones == null
                && publicUser.NameFirst != null
                && publicUser.NameSecond != null
                && publicUser.Tag != null
                && publicUser.About != null
                && publicUser.Photo != null);
        }
        [Fact]
        public async Task ApplyPrivacySettingsGroupsAndContacts()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var firstUser = UserConverter.GetUserVm(fillTestDbHelper.Users.FirstOrDefault());
            var secondUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(1).FirstOrDefault());
            var thirdUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(2).FirstOrDefault());
            var fourthUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(3).FirstOrDefault());
            await contactsService.RemoveContactsAsync(user.Contacts.Select(opt => opt.ContactId).ToList(), user.Id);
            var secondContact = await contactsService.CreateOrEditContactAsync(new ContactDto 
            { 
                ContactUserId = secondUser.Id.Value,
                UserId = firstUser.Id.Value
            });
            var thirdContact = await contactsService.CreateOrEditContactAsync(new ContactDto 
            {
                ContactUserId = thirdUser.Id.Value,
                UserId = firstUser.Id.Value
            });
            var group = await gropsService.CreateOrEditGroupAsync(new GroupDto 
            { 
                UserId = firstUser.Id.Value,
                UsersId = new List<long> { thirdContact.ContactUserId },
                PrivacySettings = int.MaxValue                
            });           
            firstUser.ContactsPrivacy[1] = true;
            var filteredBySecondUser = (await privacyService.ApplyPrivacySettingsAsync(new List<UserVm> { firstUser }, secondUser.Id)).FirstOrDefault();
            Assert.True(filteredBySecondUser.NameFirst != null 
                && filteredBySecondUser.NameSecond != null
                && filteredBySecondUser.Tag != null
                && filteredBySecondUser.About == null
                && filteredBySecondUser.Photo == null
                && filteredBySecondUser.Phones == null
                && filteredBySecondUser.Emails == null);
            var filteredByThirdUser = (await privacyService.ApplyPrivacySettingsAsync(new List<UserVm> { firstUser }, thirdUser.Id)).FirstOrDefault();
            Assert.True(filteredByThirdUser.NameFirst != null
                && filteredByThirdUser.NameSecond != null
                && filteredByThirdUser.Tag != null
                && filteredByThirdUser.About != null
                && filteredByThirdUser.Photo != null
                && filteredByThirdUser.Phones != null
                && filteredByThirdUser.Emails != null);
            var filteredByFourthUser = (await privacyService.ApplyPrivacySettingsAsync(new List<UserVm> { firstUser }, fourthUser.Id)).FirstOrDefault();
            Assert.True(filteredByFourthUser.NameFirst == null
                && filteredByFourthUser.NameSecond == null
                && filteredByFourthUser.Tag == null
                && filteredByFourthUser.About == null
                && filteredByFourthUser.Photo == null
                && filteredByFourthUser.Phones == null
                && filteredByFourthUser.Emails == null);
        }      
        [Fact]
        public async Task ApplyPrivacySettingsSearchQuery()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var firstUser = UserConverter.GetUserVm(fillTestDbHelper.Users.FirstOrDefault());
            var secondUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(1).FirstOrDefault());
            var thirdUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(2).FirstOrDefault());
            var fourthUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(3).FirstOrDefault());
            await contactsService.RemoveContactsAsync(user.Contacts.Select(opt => opt.ContactId).ToList(), user.Id);
            var secondContact = await contactsService.CreateOrEditContactAsync(new ContactDto
            {
                ContactUserId = secondUser.Id.Value,
                UserId = firstUser.Id.Value
            });
            var thirdContact = await contactsService.CreateOrEditContactAsync(new ContactDto
            {
                ContactUserId = thirdUser.Id.Value,
                UserId = firstUser.Id.Value
            });
            var group = await gropsService.CreateOrEditGroupAsync(new GroupDto
            {
                UserId = firstUser.Id.Value,
                UsersId = new List<long> { thirdContact.ContactUserId },
                PrivacySettings = int.MaxValue
            });
            firstUser.ContactsPrivacy[1] = true;
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Phones.FirstOrDefault().FullNumber, secondUser.Id));
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Emails.FirstOrDefault(), secondUser.Id));
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.NameFirst, secondUser.Id));
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.NameSecond, secondUser.Id));
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Tag, secondUser.Id));

            var filteredByThirdUser = (await privacyService.ApplyPrivacySettingsAsync(new List<UserVm> { firstUser }, thirdUser.Id)).FirstOrDefault();
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.NameFirst, thirdUser.Id));
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.NameSecond, thirdUser.Id));
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Tag, thirdUser.Id));
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Emails.FirstOrDefault(), thirdUser.Id));
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Phones.FirstOrDefault().FullNumber, thirdUser.Id));

            var filteredByFourthUser = (await privacyService.ApplyPrivacySettingsAsync(new List<UserVm> { firstUser }, fourthUser.Id)).FirstOrDefault();
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Phones.FirstOrDefault().FullNumber, fourthUser.Id));
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Emails.FirstOrDefault(), fourthUser.Id));
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.NameFirst, fourthUser.Id));
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.NameSecond, fourthUser.Id));
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Tag, fourthUser.Id));
        }
        [Fact]
        public async Task ApplyPrivacySettingPhones()
        {
            var user = fillTestDbHelper.Users.FirstOrDefault();
            var firstUser = UserConverter.GetUserVm(fillTestDbHelper.Users.FirstOrDefault());
            var secondUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(1).FirstOrDefault());
            var thirdUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(2).FirstOrDefault());
            var fourthUser = UserConverter.GetUserVm(fillTestDbHelper.Users.Skip(3).FirstOrDefault());
            await contactsService.RemoveContactsAsync(user.Contacts.Select(opt => opt.ContactId).ToList(), user.Id);
            var secondContact = await contactsService.CreateOrEditContactAsync(new ContactDto
            {
                ContactUserId = secondUser.Id.Value,
                UserId = firstUser.Id.Value
            });
            var thirdContact = await contactsService.CreateOrEditContactAsync(new ContactDto
            {
                ContactUserId = thirdUser.Id.Value,
                UserId = firstUser.Id.Value
            });
            var group = await gropsService.CreateOrEditGroupAsync(new GroupDto
            {
                UserId = firstUser.Id.Value,
                UsersId = new List<long> { thirdContact.ContactUserId },
                PrivacySettings = int.MaxValue
            });
            Assert.NotEmpty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Phones.Select(opt => opt.FullNumber).ToList(), thirdUser.Id));
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Phones.Select(opt => opt.FullNumber).ToList(), secondUser.Id));
            Assert.Empty(privacyService.ApplyPrivacySettings(new List<UserVm> { firstUser }, firstUser.Phones.Select(opt => opt.FullNumber).ToList(), fourthUser.Id));
        }
    }
}