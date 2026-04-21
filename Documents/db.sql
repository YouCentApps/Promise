SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Currencies definition

CREATE TABLE Currencies
(
    Id tinyint NOT NULL,
    Code nchar(3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Number nchar(3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Name nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT PK_Currencies_Id PRIMARY KEY (Id)
);


-- Languages definition


CREATE TABLE Languages
(
    Id int NOT NULL,
    NameEng nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    NameCode nvarchar(10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    NameNative nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT PK_Languages_Id PRIMARY KEY (Id)
);


-- Users definition


CREATE TABLE Users
(
    Id bigint IDENTITY(2523236531105401,1) NOT NULL,
    [Login] nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Password nchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Salt nvarchar(32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CreationDate datetime DEFAULT getdate() NOT NULL,
    CONSTRAINT PK_Users_Id PRIMARY KEY (Id),
    CONSTRAINT UK_Users_Login UNIQUE ([Login])
);


-- Balances definition


CREATE TABLE Balances
(
    UserId bigint NOT NULL,
    Cents bigint NOT NULL,
    CONSTRAINT PK_Balances_UserId PRIMARY KEY (UserId),
    CONSTRAINT FK_Balances_UserId_Users_Id FOREIGN KEY (UserId) REFERENCES Users(Id)
);


-- OneTimePasswords definition


CREATE TABLE OneTimePasswords
(
    UserId bigint NOT NULL,
    Value nvarchar(256) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Expiration datetime2 NOT NULL,
    CONSTRAINT PK_OneTimePasswords_UserId PRIMARY KEY (UserId),
    CONSTRAINT FK_OneTimePasswords_UserId_Users_Id FOREIGN KEY (UserId) REFERENCES Users(Id)
);


-- PromiseLimits definition


CREATE TABLE PromiseLimits
(
    UserId bigint NOT NULL,
    Cents bigint NOT NULL,
    CONSTRAINT PK_PromiseLimits_UserId PRIMARY KEY (UserId),
    CONSTRAINT FK_PromiseLimits_UserId_Users_Id FOREIGN KEY (UserId) REFERENCES Users(Id)
);


-- PromiseTransactions definition


CREATE TABLE PromiseTransactions
(
    Id bigint IDENTITY(1,1) NOT NULL,
    SenderId bigint NOT NULL,
    ReceiverId bigint NOT NULL,
    Cents int NOT NULL,
    [Date] datetime DEFAULT getutcdate() NOT NULL,
    Hash nchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    IsBlockchain bit NOT NULL,
    Memo nvarchar(256) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT PK_PromiseTransactions_Id PRIMARY KEY (Id),
    CONSTRAINT FK_PromiseTransactions_ReceiverId_Users_Id FOREIGN KEY (ReceiverId) REFERENCES Users(Id),
    CONSTRAINT FK_PromiseTransactions_SenderId_Users_Id FOREIGN KEY (SenderId) REFERENCES Users(Id)
);
CREATE NONCLUSTERED INDEX IX_PromiseTransactions_ReceiverId ON dbo.PromiseTransactions (  ReceiverId ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ]
;
CREATE NONCLUSTERED INDEX IX_PromiseTransactions_SenderId ON dbo.PromiseTransactions (  SenderId ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ]
;



-- Rates definition


CREATE TABLE Rates
(
    CurrencyId tinyint NOT NULL,
    AmountFor100 float NOT NULL,
    UpdateDate date NOT NULL,
    CONSTRAINT PK_Rates_CurrencyId PRIMARY KEY (CurrencyId),
    CONSTRAINT FK_Rates_CurrencyId_Currencies_Id FOREIGN KEY (CurrencyId) REFERENCES Currencies(Id) ON DELETE CASCADE ON UPDATE CASCADE
);


-- UserSettings definition


CREATE TABLE UserSettings
(
    UserId bigint NOT NULL,
    LanguageId int NOT NULL,
    CurrencyId tinyint NOT NULL,
    IsDarkTheme bit NOT NULL,
    CONSTRAINT PK_UserSettings_UserId PRIMARY KEY (UserId),
    CONSTRAINT FK_UserSettings_CurrencyId_Currencies_Id FOREIGN KEY (CurrencyId) REFERENCES Currencies(Id),
    CONSTRAINT FK_UserSettings_LanguageId_Languages_Id FOREIGN KEY (LanguageId) REFERENCES Languages(Id),
    CONSTRAINT FK_UserSettings_UserId_Users_Id FOREIGN KEY (UserId) REFERENCES Users(Id)
);


-- PersonalData definition


CREATE TABLE PersonalData (
	UserId bigint NOT NULL,
	Email nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Tel nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Secret nvarchar(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	EmailHash nvarchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	TelHash nvarchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	SecretHash nvarchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	Salt nvarchar(32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	EmailMasked nvarchar(150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	TelMasked nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	CONSTRAINT PK_PersonalData_UserId PRIMARY KEY (UserId)
);

ALTER TABLE PersonalData ADD CONSTRAINT FK_PersonalData_UserId_Users_Id FOREIGN KEY (UserId) REFERENCES YCDB.dbo.Users(Id);


-- AccessRestore definition


CREATE TABLE AccessRestore (
	UserId bigint NOT NULL,
	UseSecretTryNumber int NOT NULL,
	UseSecretTryDate datetime NULL,
	UseEmailTryNumber int NOT NULL,
	UseEmailTryDate datetime NULL,
	UseTelTryNumber int NOT NULL,
	UseTelTryDate datetime NULL,
	CONSTRAINT PK_AccessRestore_UserId PRIMARY KEY (UserId)
);

ALTER TABLE AccessRestore ADD CONSTRAINT FK_AccessRestore_UserId_User_Id FOREIGN KEY (UserId) REFERENCES Users(Id);






-- LOOKUP TABLES for Merchant operations


CREATE TABLE MerchantPaymentRequestTypes
(
    Id tinyint NOT NULL,
    Name nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT PK_MerchantPaymentRequestTypes_Id PRIMARY KEY (Id)
);

INSERT INTO MerchantPaymentRequestTypes (Id, Name) VALUES (1, N'OneTime'), (2, N'Subscription');


CREATE TABLE MerchantPaymentRequestStatuses
(
    Id tinyint NOT NULL,
    Name nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT PK_MerchantPaymentRequestStatuses_Id PRIMARY KEY (Id)
);

INSERT INTO MerchantPaymentRequestStatuses (Id, Name) VALUES (1, N'Pending'), (2, N'Completed'), (3, N'Expired'), (4, N'Cancelled');


CREATE TABLE MerchantSubscriptionStatuses
(
    Id tinyint NOT NULL,
    Name nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT PK_MerchantSubscriptionStatuses_Id PRIMARY KEY (Id)
);

INSERT INTO MerchantSubscriptionStatuses (Id, Name) VALUES (1, N'Active'), (2, N'Cancelled'), (3, N'Expired');


CREATE TABLE MerchantTransactionTypes
(
    Id tinyint NOT NULL,
    Name nvarchar(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT PK_MerchantTransactionTypes_Id PRIMARY KEY (Id)
);

INSERT INTO MerchantTransactionTypes (Id, Name) VALUES (1, N'Charge'), (2, N'Refund');




-- Merchants: a User who is also a merchant

CREATE TABLE Merchants
(
    UserId bigint NOT NULL,
    Name nvarchar(100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Website nvarchar(256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    ApiKey nvarchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    ApiSecretHash nchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Salt nvarchar(32) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    IsActive bit NOT NULL,
    CreatedDate datetime DEFAULT getdate() NOT NULL,
    CONSTRAINT PK_Merchants_UserId PRIMARY KEY (UserId),
    CONSTRAINT FK_Merchants_UserId_Users_Id FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UK_Merchants_ApiKey UNIQUE (ApiKey)
);


-- MerchantPaymentRequests: checkout sessions created by merchants

CREATE TABLE MerchantPaymentRequests
(
    Id bigint IDENTITY(1,1) NOT NULL,
    Token nvarchar(64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    MerchantId bigint NOT NULL,
    AmountCents int NOT NULL,
    Description nvarchar(256) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    TypeId tinyint NOT NULL,
    StatusId tinyint NOT NULL,
    CallbackUrl nvarchar(512) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CreatedDate datetime DEFAULT getutcdate() NOT NULL,
    ExpiresDate datetime NULL,
    IntervalDays int NULL,
    SubscriptionExpiresDate datetime NULL,
    CONSTRAINT PK_MerchantPaymentRequests_Id PRIMARY KEY (Id),
    CONSTRAINT FK_MerchantPaymentRequests_MerchantId_Users_Id FOREIGN KEY (MerchantId) REFERENCES Users(Id),
    CONSTRAINT FK_MerchantPaymentRequests_TypeId_MerchantPaymentRequestTypes_Id FOREIGN KEY (TypeId) REFERENCES MerchantPaymentRequestTypes(Id),
    CONSTRAINT FK_MerchantPaymentRequests_StatusId_MerchantPaymentRequestStatuses_Id FOREIGN KEY (StatusId) REFERENCES MerchantPaymentRequestStatuses(Id),
    CONSTRAINT UK_MerchantPaymentRequests_Token UNIQUE (Token)
);

CREATE NONCLUSTERED INDEX IX_MerchantPaymentRequests_MerchantId ON MerchantPaymentRequests (MerchantId ASC);


-- MerchantSubscriptions: active consent-based recurring charge agreements

CREATE TABLE MerchantSubscriptions
(
    Id bigint IDENTITY(1,1) NOT NULL,
    MerchantId bigint NOT NULL,
    SubscriberId bigint NOT NULL,
    PaymentRequestId bigint NOT NULL,
    AmountCents int NOT NULL,
    IntervalDays int NOT NULL,
    NextChargeDate datetime NOT NULL,
    ExpiresDate datetime NULL,
    StatusId tinyint NOT NULL,
    ConsentDate datetime NOT NULL,
    CancelledDate datetime NULL,
    CONSTRAINT PK_MerchantSubscriptions_Id PRIMARY KEY (Id),
    CONSTRAINT FK_MerchantSubscriptions_MerchantId_Users_Id FOREIGN KEY (MerchantId) REFERENCES Users(Id),
    CONSTRAINT FK_MerchantSubscriptions_SubscriberId_Users_Id FOREIGN KEY (SubscriberId) REFERENCES Users(Id),
    CONSTRAINT FK_MerchantSubscriptions_PaymentRequestId_MerchantPaymentRequests_Id FOREIGN KEY (PaymentRequestId) REFERENCES MerchantPaymentRequests(Id),
    CONSTRAINT FK_MerchantSubscriptions_StatusId_MerchantSubscriptionStatuses_Id FOREIGN KEY (StatusId) REFERENCES MerchantSubscriptionStatuses(Id)
);

CREATE NONCLUSTERED INDEX IX_MerchantSubscriptions_MerchantId ON MerchantSubscriptions (MerchantId ASC);
CREATE NONCLUSTERED INDEX IX_MerchantSubscriptions_SubscriberId ON MerchantSubscriptions (SubscriberId ASC);


-- MerchantTransactions: audit log of every charge and refund

CREATE TABLE MerchantTransactions
(
    Id bigint IDENTITY(1,1) NOT NULL,
    MerchantId bigint NOT NULL,
    PayerId bigint NOT NULL,
    SubscriptionId bigint NULL,
    PaymentRequestId bigint NOT NULL,
    PromiseTransactionId bigint NOT NULL,
    AmountCents int NOT NULL,
    TypeId tinyint NOT NULL,
    [Date] datetime DEFAULT getutcdate() NOT NULL,
    CONSTRAINT PK_MerchantTransactions_Id PRIMARY KEY (Id),
    CONSTRAINT FK_MerchantTransactions_MerchantId_Users_Id FOREIGN KEY (MerchantId) REFERENCES Users(Id),
    CONSTRAINT FK_MerchantTransactions_PayerId_Users_Id FOREIGN KEY (PayerId) REFERENCES Users(Id),
    CONSTRAINT FK_MerchantTransactions_SubscriptionId_MerchantSubscriptions_Id FOREIGN KEY (SubscriptionId) REFERENCES MerchantSubscriptions(Id),
    CONSTRAINT FK_MerchantTransactions_PaymentRequestId_MerchantPaymentRequests_Id FOREIGN KEY (PaymentRequestId) REFERENCES MerchantPaymentRequests(Id),
    CONSTRAINT FK_MerchantTransactions_PromiseTransactionId_PromiseTransactions_Id FOREIGN KEY (PromiseTransactionId) REFERENCES PromiseTransactions(Id),
    CONSTRAINT FK_MerchantTransactions_TypeId_MerchantTransactionTypes_Id FOREIGN KEY (TypeId) REFERENCES MerchantTransactionTypes(Id)
);

CREATE NONCLUSTERED INDEX IX_MerchantTransactions_MerchantId ON MerchantTransactions (MerchantId ASC);
CREATE NONCLUSTERED INDEX IX_MerchantTransactions_PayerId ON MerchantTransactions (PayerId ASC);
CREATE NONCLUSTERED INDEX IX_MerchantTransactions_SubscriptionId ON MerchantTransactions (SubscriptionId ASC);




