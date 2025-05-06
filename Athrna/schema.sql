IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [City] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_City] PRIMARY KEY ([Id])
);

CREATE TABLE [Client] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [EncryptedPassword] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [IsEmailVerified] bit NOT NULL,
    [IsBanned] bit NOT NULL,
    [BanReason] nvarchar(max) NULL,
    [BannedAt] datetime2 NULL,
    CONSTRAINT [PK_Client] PRIMARY KEY ([Id])
);

CREATE TABLE [Translation] (
    [Id] int NOT NULL IDENTITY,
    [SourceLanguage] nvarchar(10) NOT NULL,
    [TargetLanguage] nvarchar(10) NOT NULL,
    [OriginalText] nvarchar(max) NOT NULL,
    [TranslatedText] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [TextHash] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Translation] PRIMARY KEY ([Id])
);

CREATE TABLE [Guide] (
    [Id] int NOT NULL IDENTITY,
    [CityId] int NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [NationalId] nvarchar(max) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Guide] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Guide_City_CityId] FOREIGN KEY ([CityId]) REFERENCES [City] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [GuideApplication] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [CityId] int NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [NationalId] nvarchar(max) NOT NULL,
    [LicenseNumber] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RejectionReason] nvarchar(max) NULL,
    [SubmissionDate] datetime2 NOT NULL,
    [ReviewDate] datetime2 NULL,
    CONSTRAINT [PK_GuideApplication] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GuideApplication_City_CityId] FOREIGN KEY ([CityId]) REFERENCES [City] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Site] (
    [Id] int NOT NULL IDENTITY,
    [CityId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Location] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [SiteType] nvarchar(max) NOT NULL,
    [ImagePath] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Site] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Site_City_CityId] FOREIGN KEY ([CityId]) REFERENCES [City] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Administrator] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [RoleLevel] int NOT NULL,
    CONSTRAINT [PK_Administrator] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Administrator_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Client] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [GuideAvailability] (
    [Id] int NOT NULL IDENTITY,
    [GuideId] int NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [IsAvailable] bit NOT NULL,
    CONSTRAINT [PK_GuideAvailability] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GuideAvailability_Guide_GuideId] FOREIGN KEY ([GuideId]) REFERENCES [Guide] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Message] (
    [Id] int NOT NULL IDENTITY,
    [SenderId] int NOT NULL,
    [SenderType] nvarchar(max) NOT NULL,
    [RecipientId] int NOT NULL,
    [RecipientType] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    [IsRead] bit NOT NULL,
    [ClientId] int NULL,
    [GuideId] int NULL,
    CONSTRAINT [PK_Message] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Message_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Client] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Message_Guide_GuideId] FOREIGN KEY ([GuideId]) REFERENCES [Guide] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Booking] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [GuideId] int NOT NULL,
    [SiteId] int NULL,
    [BookingDate] datetime2 NOT NULL,
    [TourDateTime] datetime2 NOT NULL,
    [GroupSize] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Booking] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Booking_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Client] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Booking_Guide_GuideId] FOREIGN KEY ([GuideId]) REFERENCES [Guide] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Booking_Site_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Site] ([Id])
);

CREATE TABLE [Bookmark] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [SiteId] int NOT NULL,
    CONSTRAINT [PK_Bookmark] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bookmark_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Client] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Bookmark_Site_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Site] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [CulturalInfo] (
    [Id] int NOT NULL IDENTITY,
    [SiteId] int NOT NULL,
    [Summary] nvarchar(max) NOT NULL,
    [EstablishedDate] int NOT NULL,
    CONSTRAINT [PK_CulturalInfo] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CulturalInfo_Site_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Site] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Rating] (
    [Id] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [SiteId] int NOT NULL,
    [Value] int NOT NULL,
    [Review] nvarchar(max) NULL,
    CONSTRAINT [PK_Rating] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rating_Client_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Client] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Rating_Site_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Site] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Service] (
    [Id] int NOT NULL IDENTITY,
    [SiteId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NOT NULL,
    [IconName] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Service] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Service_Site_SiteId] FOREIGN KEY ([SiteId]) REFERENCES [Site] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Administrator_ClientId] ON [Administrator] ([ClientId]);

CREATE INDEX [IX_Booking_ClientId] ON [Booking] ([ClientId]);

CREATE INDEX [IX_Booking_GuideId] ON [Booking] ([GuideId]);

CREATE INDEX [IX_Booking_SiteId] ON [Booking] ([SiteId]);

CREATE INDEX [IX_Bookmark_ClientId] ON [Bookmark] ([ClientId]);

CREATE INDEX [IX_Bookmark_SiteId] ON [Bookmark] ([SiteId]);

CREATE UNIQUE INDEX [IX_Client_Email] ON [Client] ([Email]);

CREATE UNIQUE INDEX [IX_Client_Username] ON [Client] ([Username]);

CREATE UNIQUE INDEX [IX_CulturalInfo_SiteId] ON [CulturalInfo] ([SiteId]);

CREATE INDEX [IX_Guide_CityId] ON [Guide] ([CityId]);

CREATE INDEX [IX_GuideApplication_CityId] ON [GuideApplication] ([CityId]);

CREATE INDEX [IX_GuideAvailability_GuideId] ON [GuideAvailability] ([GuideId]);

CREATE INDEX [IX_Message_ClientId] ON [Message] ([ClientId]);

CREATE INDEX [IX_Message_GuideId] ON [Message] ([GuideId]);

CREATE INDEX [IX_Rating_ClientId] ON [Rating] ([ClientId]);

CREATE INDEX [IX_Rating_SiteId] ON [Rating] ([SiteId]);

CREATE INDEX [IX_Service_SiteId] ON [Service] ([SiteId]);

CREATE INDEX [IX_Site_CityId] ON [Site] ([CityId]);

CREATE UNIQUE INDEX [IX_Translation_SourceLanguage_TargetLanguage_TextHash] ON [Translation] ([SourceLanguage], [TargetLanguage], [TextHash]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250506192604_InitialCreate', N'9.0.3');

COMMIT;
GO