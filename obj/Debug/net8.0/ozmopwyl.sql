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
CREATE TABLE [Admins] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Admins] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(50) NOT NULL,
    [LastName] nvarchar(50) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(15) NULL,
    [ProfileImagePath] nvarchar(max) NULL,
    [IsAdmin] bit NOT NULL,
    [IsEmailVerified] bit NOT NULL,
    [IsIdVerified] bit NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [AdminActions] (
    [Id] int NOT NULL IDENTITY,
    [AdminId] int NOT NULL,
    [ActionType] nvarchar(50) NOT NULL,
    [EntityType] nvarchar(50) NOT NULL,
    [EntityId] int NULL,
    [ActionDetails] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_AdminActions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdminActions_Admins_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Admins] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [EmailVerifications] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(max) NOT NULL,
    [Code] nvarchar(6) NOT NULL,
    [Expiration] datetime2 NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_EmailVerifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmailVerifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Listings] (
    [Id] int NOT NULL IDENTITY,
    [SellerId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Condition] nvarchar(max) NOT NULL,
    [Stock] int NULL,
    [Category] nvarchar(max) NOT NULL,
    [SubCategory] nvarchar(max) NOT NULL,
    [DetailCategory] nvarchar(max) NOT NULL,
    [Location] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RejectionReason] nvarchar(max) NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Listings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Listings_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Type] int NOT NULL,
    [RelatedEntityId] int NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRatings] (
    [Id] int NOT NULL IDENTITY,
    [RaterId] int NOT NULL,
    [RatedUserId] int NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_UserRatings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRatings_Users_RatedUserId] FOREIGN KEY ([RatedUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRatings_Users_RaterId] FOREIGN KEY ([RaterId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [VerificationDocuments] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [FrontImagePath] nvarchar(max) NOT NULL,
    [BackImagePath] nvarchar(max) NOT NULL,
    [FaceImagePath] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [RejectionReason] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_VerificationDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VerificationDocuments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AndroidPhones] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [FaceRecognition] bit NOT NULL,
    [Camera] nvarchar(max) NULL,
    [BatteryPower] nvarchar(max) NULL,
    [ScreenSize] nvarchar(max) NULL,
    [ChargingPort] nvarchar(max) NULL,
    [Ram] nvarchar(max) NULL,
    [Storage] nvarchar(max) NULL,
    [Warranty] nvarchar(max) NULL,
    CONSTRAINT [PK_AndroidPhones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AndroidPhones_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AndroidTablets] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [FaceRecognition] bit NOT NULL,
    [Camera] nvarchar(max) NOT NULL,
    [BatteryPower] nvarchar(max) NOT NULL,
    [ScreenSize] nvarchar(max) NOT NULL,
    [ChargingPort] nvarchar(max) NOT NULL,
    [Ram] nvarchar(max) NOT NULL,
    [Storage] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_AndroidTablets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AndroidTablets_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [BeveragePreparations] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_BeveragePreparations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BeveragePreparations_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Bookmarks] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ListingId] int NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Bookmarks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bookmarks_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Bookmarks_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Cameras] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Cameras] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Cameras_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Computer] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [Ram] nvarchar(max) NOT NULL,
    [RamType] nvarchar(max) NOT NULL,
    [Hdmi] bit NOT NULL,
    [GpuMemory] nvarchar(max) NOT NULL,
    [Gpu] nvarchar(max) NOT NULL,
    [MemorySpeed] nvarchar(max) NOT NULL,
    [Processor] nvarchar(max) NOT NULL,
    [OperatingSystem] nvarchar(max) NOT NULL,
    [Storage] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Computer] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Computer_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ComputerAccessories] (
    [Id] int NOT NULL IDENTITY,
    [CompatibleModels] nvarchar(max) NOT NULL,
    [ConnectionType] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_ComputerAccessories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComputerAccessories_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ComputerBags] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_ComputerBags] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComputerBags_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ComputerComponents] (
    [Id] int NOT NULL IDENTITY,
    [CompatibleModels] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_ComputerComponents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ComputerComponents_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Desktops] (
    [Id] int NOT NULL IDENTITY,
    [Keyboard] nvarchar(max) NOT NULL,
    [ScreenSize] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    [Ram] nvarchar(max) NOT NULL,
    [RamType] nvarchar(max) NOT NULL,
    [Hdmi] bit NOT NULL,
    [GpuMemory] nvarchar(max) NOT NULL,
    [Gpu] nvarchar(max) NOT NULL,
    [MemorySpeed] nvarchar(max) NOT NULL,
    [Processor] nvarchar(max) NOT NULL,
    [OperatingSystem] nvarchar(max) NOT NULL,
    [Storage] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Desktops] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Desktops_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Dishwashers] (
    [Id] int NOT NULL IDENTITY,
    [Capacity] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    [EnergyClass] nvarchar(max) NOT NULL,
    [Dimensions] nvarchar(max) NOT NULL,
    [Color] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Dishwashers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Dishwashers_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [FoodPreparations] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_FoodPreparations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FoodPreparations_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Fridges] (
    [Id] int NOT NULL IDENTITY,
    [Volume] nvarchar(max) NOT NULL,
    [Freezer] bit NOT NULL,
    [ListingId] int NOT NULL,
    [EnergyClass] nvarchar(max) NOT NULL,
    [Dimensions] nvarchar(max) NOT NULL,
    [Color] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Fridges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Fridges_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [HeadphonesEarphones] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_HeadphonesEarphones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HeadphonesEarphones_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [HeatingCoolings] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_HeatingCoolings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HeatingCoolings_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [IOSPhones] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [FaceRecognition] bit NOT NULL,
    [Camera] nvarchar(max) NULL,
    [BatteryPower] nvarchar(max) NULL,
    [ScreenSize] nvarchar(max) NULL,
    [ChargingPort] nvarchar(max) NULL,
    [Ram] nvarchar(max) NULL,
    [Storage] nvarchar(max) NULL,
    [Warranty] nvarchar(max) NULL,
    CONSTRAINT [PK_IOSPhones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IOSPhones_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [IOSTablets] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [FaceRecognition] bit NOT NULL,
    [Camera] nvarchar(max) NOT NULL,
    [BatteryPower] nvarchar(max) NOT NULL,
    [ScreenSize] nvarchar(max) NOT NULL,
    [ChargingPort] nvarchar(max) NOT NULL,
    [Ram] nvarchar(max) NOT NULL,
    [Storage] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_IOSTablets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_IOSTablets_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Irons] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Irons] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Irons_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Keyboards] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Keyboards] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Keyboards_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Laptops] (
    [Id] int NOT NULL IDENTITY,
    [Keyboard] nvarchar(max) NOT NULL,
    [ScreenSize] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    [Ram] nvarchar(max) NOT NULL,
    [RamType] nvarchar(max) NOT NULL,
    [Hdmi] bit NOT NULL,
    [GpuMemory] nvarchar(max) NOT NULL,
    [Gpu] nvarchar(max) NOT NULL,
    [MemorySpeed] nvarchar(max) NOT NULL,
    [Processor] nvarchar(max) NOT NULL,
    [OperatingSystem] nvarchar(max) NOT NULL,
    [Storage] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Laptops] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Laptops_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ListingImages] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [ImagePath] nvarchar(max) NOT NULL,
    [IsMainImage] bit NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ListingImages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ListingImages_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Messages] (
    [Id] int NOT NULL IDENTITY,
    [SenderId] int NOT NULL,
    [ReceiverId] int NOT NULL,
    [ListingId] int NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Messages_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Messages_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Microphones] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Microphones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Microphones_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [MicrowaveOvens] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_MicrowaveOvens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MicrowaveOvens_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Monitors] (
    [Id] int NOT NULL IDENTITY,
    [CompatibleModels] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Monitors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Monitors_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Mouses] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Mouses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Mouses_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OtherPhones] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [FaceRecognition] bit NOT NULL,
    [Camera] nvarchar(max) NULL,
    [BatteryPower] nvarchar(max) NULL,
    [ScreenSize] nvarchar(max) NULL,
    [ChargingPort] nvarchar(max) NULL,
    [Ram] nvarchar(max) NULL,
    [Storage] nvarchar(max) NULL,
    [Warranty] nvarchar(max) NULL,
    CONSTRAINT [PK_OtherPhones] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OtherPhones_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OtherTablets] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [FaceRecognition] bit NOT NULL,
    [Camera] nvarchar(max) NOT NULL,
    [BatteryPower] nvarchar(max) NOT NULL,
    [ScreenSize] nvarchar(max) NOT NULL,
    [ChargingPort] nvarchar(max) NOT NULL,
    [Ram] nvarchar(max) NOT NULL,
    [Storage] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_OtherTablets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_OtherTablets_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Ovens] (
    [Id] int NOT NULL IDENTITY,
    [Timer] bit NOT NULL,
    [Volume] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    [EnergyClass] nvarchar(max) NOT NULL,
    [Dimensions] nvarchar(max) NOT NULL,
    [Color] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Ovens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Ovens_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PhoneAccessories] (
    [Id] int NOT NULL IDENTITY,
    [CompatibleModels] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_PhoneAccessories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PhoneAccessories_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ProductReviews] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    [UserId] int NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(1000) NOT NULL,
    [IsVerifiedPurchase] bit NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProductReviews_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProductReviews_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SewingMachines] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_SewingMachines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SewingMachines_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SpareParts] (
    [Id] int NOT NULL IDENTITY,
    [CompatibleModels] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_SpareParts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SpareParts_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Speakers] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Speakers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Speakers_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Stoves] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Stoves] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Stoves_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [TabletAccessories] (
    [Id] int NOT NULL IDENTITY,
    [CompatibleModels] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_TabletAccessories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TabletAccessories_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Televisions] (
    [Id] int NOT NULL IDENTITY,
    [ScreenSize] nvarchar(max) NOT NULL,
    [SmartTV] bit NOT NULL,
    [Hdmi] nvarchar(max) NOT NULL,
    [Resolution] nvarchar(max) NOT NULL,
    [DisplayTechnology] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Televisions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Televisions_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [VacuumCleaners] (
    [Id] int NOT NULL IDENTITY,
    [CableLength] nvarchar(max) NOT NULL,
    [WaterContainer] bit NOT NULL,
    [Weight] nvarchar(max) NOT NULL,
    [Power] nvarchar(max) NOT NULL,
    [DustContainer] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_VacuumCleaners] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VacuumCleaners_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Washers] (
    [Id] int NOT NULL IDENTITY,
    [Capacity] nvarchar(max) NOT NULL,
    [ListingId] int NOT NULL,
    [EnergyClass] nvarchar(max) NOT NULL,
    [Dimensions] nvarchar(max) NOT NULL,
    [Color] nvarchar(max) NOT NULL,
    [Warranty] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Washers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Washers_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Webcams] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Webcams] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Webcams_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Offers] (
    [Id] int NOT NULL IDENTITY,
    [BuyerId] int NOT NULL,
    [ListingId] int NOT NULL,
    [OfferAmount] decimal(18,2) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [MessageId] int NULL,
    [IsCounterOffer] bit NOT NULL,
    [OriginalOfferId] int NULL,
    [ResponseDate] datetime2 NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Offers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Offers_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Offers_Messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [Messages] ([Id]),
    CONSTRAINT [FK_Offers_Offers_OriginalOfferId] FOREIGN KEY ([OriginalOfferId]) REFERENCES [Offers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Offers_Users_BuyerId] FOREIGN KEY ([BuyerId]) REFERENCES [Users] ([Id])
);

CREATE TABLE [Purchases] (
    [Id] int NOT NULL IDENTITY,
    [BuyerId] int NOT NULL,
    [ListingId] int NOT NULL,
    [OfferId] int NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [PaymentStatus] int NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Purchases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Purchases_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Purchases_Offers_OfferId] FOREIGN KEY ([OfferId]) REFERENCES [Offers] ([Id]),
    CONSTRAINT [FK_Purchases_Users_BuyerId] FOREIGN KEY ([BuyerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [PurchaseConfirmations] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseId] int NOT NULL,
    [SellerShippedProduct] bit NOT NULL,
    [ShippingConfirmedDate] datetime2 NULL,
    [BuyerReceivedProduct] bit NOT NULL,
    [ReceiptConfirmedDate] datetime2 NULL,
    [PaymentReleased] bit NOT NULL,
    [PaymentReleasedDate] datetime2 NULL,
    [TrackingNumber] nvarchar(max) NULL,
    [ShippingProvider] nvarchar(max) NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_PurchaseConfirmations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseConfirmations_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SellerRatings] (
    [Id] int NOT NULL IDENTITY,
    [BuyerId] int NOT NULL,
    [SellerId] int NOT NULL,
    [Rating] int NOT NULL,
    [PurchaseId] int NOT NULL,
    [CreatedAt] datetime2 NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_SellerRatings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SellerRatings_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SellerRatings_Users_BuyerId] FOREIGN KEY ([BuyerId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_SellerRatings_Users_SellerId] FOREIGN KEY ([SellerId]) REFERENCES [Users] ([Id])
);

CREATE INDEX [IX_AdminActions_AdminId] ON [AdminActions] ([AdminId]);

CREATE UNIQUE INDEX [IX_Admins_Username] ON [Admins] ([Username]);

CREATE UNIQUE INDEX [IX_AndroidPhones_ListingId] ON [AndroidPhones] ([ListingId]);

CREATE UNIQUE INDEX [IX_AndroidTablets_ListingId] ON [AndroidTablets] ([ListingId]);

CREATE UNIQUE INDEX [IX_BeveragePreparations_ListingId] ON [BeveragePreparations] ([ListingId]);

CREATE INDEX [IX_Bookmarks_ListingId] ON [Bookmarks] ([ListingId]);

CREATE UNIQUE INDEX [IX_Bookmarks_UserId_ListingId] ON [Bookmarks] ([UserId], [ListingId]);

CREATE UNIQUE INDEX [IX_Cameras_ListingId] ON [Cameras] ([ListingId]);

CREATE UNIQUE INDEX [IX_Computer_ListingId] ON [Computer] ([ListingId]);

CREATE UNIQUE INDEX [IX_ComputerAccessories_ListingId] ON [ComputerAccessories] ([ListingId]);

CREATE UNIQUE INDEX [IX_ComputerBags_ListingId] ON [ComputerBags] ([ListingId]);

CREATE UNIQUE INDEX [IX_ComputerComponents_ListingId] ON [ComputerComponents] ([ListingId]);

CREATE UNIQUE INDEX [IX_Desktops_ListingId] ON [Desktops] ([ListingId]);

CREATE UNIQUE INDEX [IX_Dishwashers_ListingId] ON [Dishwashers] ([ListingId]);

CREATE INDEX [IX_EmailVerifications_UserId] ON [EmailVerifications] ([UserId]);

CREATE UNIQUE INDEX [IX_FoodPreparations_ListingId] ON [FoodPreparations] ([ListingId]);

CREATE UNIQUE INDEX [IX_Fridges_ListingId] ON [Fridges] ([ListingId]);

CREATE UNIQUE INDEX [IX_HeadphonesEarphones_ListingId] ON [HeadphonesEarphones] ([ListingId]);

CREATE UNIQUE INDEX [IX_HeatingCoolings_ListingId] ON [HeatingCoolings] ([ListingId]);

CREATE UNIQUE INDEX [IX_IOSPhones_ListingId] ON [IOSPhones] ([ListingId]);

CREATE UNIQUE INDEX [IX_IOSTablets_ListingId] ON [IOSTablets] ([ListingId]);

CREATE UNIQUE INDEX [IX_Irons_ListingId] ON [Irons] ([ListingId]);

CREATE UNIQUE INDEX [IX_Keyboards_ListingId] ON [Keyboards] ([ListingId]);

CREATE UNIQUE INDEX [IX_Laptops_ListingId] ON [Laptops] ([ListingId]);

CREATE INDEX [IX_ListingImages_ListingId] ON [ListingImages] ([ListingId]);

CREATE INDEX [IX_Listings_SellerId] ON [Listings] ([SellerId]);

CREATE INDEX [IX_Messages_ListingId] ON [Messages] ([ListingId]);

CREATE INDEX [IX_Messages_ReceiverId] ON [Messages] ([ReceiverId]);

CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);

CREATE UNIQUE INDEX [IX_Microphones_ListingId] ON [Microphones] ([ListingId]);

CREATE UNIQUE INDEX [IX_MicrowaveOvens_ListingId] ON [MicrowaveOvens] ([ListingId]);

CREATE UNIQUE INDEX [IX_Monitors_ListingId] ON [Monitors] ([ListingId]);

CREATE UNIQUE INDEX [IX_Mouses_ListingId] ON [Mouses] ([ListingId]);

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);

CREATE INDEX [IX_Offers_BuyerId] ON [Offers] ([BuyerId]);

CREATE INDEX [IX_Offers_ListingId] ON [Offers] ([ListingId]);

CREATE UNIQUE INDEX [IX_Offers_MessageId] ON [Offers] ([MessageId]) WHERE [MessageId] IS NOT NULL;

CREATE INDEX [IX_Offers_OriginalOfferId] ON [Offers] ([OriginalOfferId]);

CREATE UNIQUE INDEX [IX_OtherPhones_ListingId] ON [OtherPhones] ([ListingId]);

CREATE UNIQUE INDEX [IX_OtherTablets_ListingId] ON [OtherTablets] ([ListingId]);

CREATE UNIQUE INDEX [IX_Ovens_ListingId] ON [Ovens] ([ListingId]);

CREATE UNIQUE INDEX [IX_PhoneAccessories_ListingId] ON [PhoneAccessories] ([ListingId]);

CREATE INDEX [IX_ProductReviews_ListingId] ON [ProductReviews] ([ListingId]);

CREATE UNIQUE INDEX [IX_ProductReviews_UserId_ListingId] ON [ProductReviews] ([UserId], [ListingId]);

CREATE UNIQUE INDEX [IX_PurchaseConfirmations_PurchaseId] ON [PurchaseConfirmations] ([PurchaseId]);

CREATE INDEX [IX_Purchases_BuyerId] ON [Purchases] ([BuyerId]);

CREATE INDEX [IX_Purchases_ListingId] ON [Purchases] ([ListingId]);

CREATE UNIQUE INDEX [IX_Purchases_OfferId] ON [Purchases] ([OfferId]) WHERE [OfferId] IS NOT NULL;

CREATE UNIQUE INDEX [IX_SellerRatings_BuyerId_SellerId_PurchaseId] ON [SellerRatings] ([BuyerId], [SellerId], [PurchaseId]);

CREATE INDEX [IX_SellerRatings_PurchaseId] ON [SellerRatings] ([PurchaseId]);

CREATE INDEX [IX_SellerRatings_SellerId] ON [SellerRatings] ([SellerId]);

CREATE UNIQUE INDEX [IX_SewingMachines_ListingId] ON [SewingMachines] ([ListingId]);

CREATE UNIQUE INDEX [IX_SpareParts_ListingId] ON [SpareParts] ([ListingId]);

CREATE UNIQUE INDEX [IX_Speakers_ListingId] ON [Speakers] ([ListingId]);

CREATE UNIQUE INDEX [IX_Stoves_ListingId] ON [Stoves] ([ListingId]);

CREATE UNIQUE INDEX [IX_TabletAccessories_ListingId] ON [TabletAccessories] ([ListingId]);

CREATE UNIQUE INDEX [IX_Televisions_ListingId] ON [Televisions] ([ListingId]);

CREATE INDEX [IX_UserRatings_RatedUserId] ON [UserRatings] ([RatedUserId]);

CREATE UNIQUE INDEX [IX_UserRatings_RaterId_RatedUserId] ON [UserRatings] ([RaterId], [RatedUserId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_VacuumCleaners_ListingId] ON [VacuumCleaners] ([ListingId]);

CREATE UNIQUE INDEX [IX_VerificationDocuments_UserId] ON [VerificationDocuments] ([UserId]);

CREATE UNIQUE INDEX [IX_Washers_ListingId] ON [Washers] ([ListingId]);

CREATE UNIQUE INDEX [IX_Webcams_ListingId] ON [Webcams] ([ListingId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250513164853_InitialCreate', N'9.0.4');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250513165617_V2', N'9.0.4');

ALTER TABLE [EmailVerifications] DROP CONSTRAINT [FK_EmailVerifications_Users_UserId];

DROP INDEX [IX_EmailVerifications_UserId] ON [EmailVerifications];

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EmailVerifications]') AND [c].[name] = N'UserId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [EmailVerifications] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [EmailVerifications] DROP COLUMN [UserId];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250513171017_MigrationV4', N'9.0.4');

DROP TABLE [Computer];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250514053519_NewmlyMigrated', N'9.0.4');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Listings]') AND [c].[name] = N'Title');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Listings] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Listings] ALTER COLUMN [Title] nvarchar(max) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250514172452_Hayde', N'9.0.4');

CREATE TABLE [Others] (
    [Id] int NOT NULL IDENTITY,
    [ListingId] int NOT NULL,
    CONSTRAINT [PK_Others] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Others_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Others_ListingId] ON [Others] ([ListingId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250516122758_HaydeHayde', N'9.0.4');

ALTER TABLE [Listings] ADD [ViewCount] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250516140025_Nugget', N'9.0.4');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250516173340_LatestVersion', N'9.0.4');

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SpareParts]') AND [c].[name] = N'CompatibleModels');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SpareParts] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [SpareParts] DROP COLUMN [CompatibleModels];

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SpareParts]') AND [c].[name] = N'Warranty');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SpareParts] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [SpareParts] DROP COLUMN [Warranty];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250520140906_UpdateOneMoreTime', N'9.0.4');

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PhoneAccessories]') AND [c].[name] = N'CompatibleModels');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [PhoneAccessories] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [PhoneAccessories] DROP COLUMN [CompatibleModels];

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PhoneAccessories]') AND [c].[name] = N'Warranty');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [PhoneAccessories] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [PhoneAccessories] DROP COLUMN [Warranty];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250520142209_PumPumPumMaxVerstappen', N'9.0.4');

ALTER TABLE [ComputerAccessories] DROP CONSTRAINT [FK_ComputerAccessories_Listings_ListingId];

ALTER TABLE [PhoneAccessories] DROP CONSTRAINT [FK_PhoneAccessories_Listings_ListingId];

ALTER TABLE [TabletAccessories] DROP CONSTRAINT [FK_TabletAccessories_Listings_ListingId];

ALTER TABLE [TabletAccessories] DROP CONSTRAINT [PK_TabletAccessories];

ALTER TABLE [PhoneAccessories] DROP CONSTRAINT [PK_PhoneAccessories];

ALTER TABLE [ComputerAccessories] DROP CONSTRAINT [PK_ComputerAccessories];

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Monitors]') AND [c].[name] = N'CompatibleModels');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Monitors] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Monitors] DROP COLUMN [CompatibleModels];

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Monitors]') AND [c].[name] = N'Warranty');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Monitors] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Monitors] DROP COLUMN [Warranty];

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerComponents]') AND [c].[name] = N'CompatibleModels');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ComputerComponents] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [ComputerComponents] DROP COLUMN [CompatibleModels];

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerComponents]') AND [c].[name] = N'Warranty');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ComputerComponents] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [ComputerComponents] DROP COLUMN [Warranty];

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TabletAccessories]') AND [c].[name] = N'CompatibleModels');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [TabletAccessories] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [TabletAccessories] DROP COLUMN [CompatibleModels];

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TabletAccessories]') AND [c].[name] = N'Warranty');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [TabletAccessories] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [TabletAccessories] DROP COLUMN [Warranty];

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerAccessories]') AND [c].[name] = N'CompatibleModels');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [ComputerAccessories] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [ComputerAccessories] DROP COLUMN [CompatibleModels];

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerAccessories]') AND [c].[name] = N'ConnectionType');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [ComputerAccessories] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [ComputerAccessories] DROP COLUMN [ConnectionType];

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ComputerAccessories]') AND [c].[name] = N'Warranty');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [ComputerAccessories] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [ComputerAccessories] DROP COLUMN [Warranty];

EXEC sp_rename N'[TabletAccessories]', N'TabletAccessorys', 'OBJECT';

EXEC sp_rename N'[PhoneAccessories]', N'PhoneAccessorys', 'OBJECT';

EXEC sp_rename N'[ComputerAccessories]', N'ComputerAccessorys', 'OBJECT';

EXEC sp_rename N'[TabletAccessorys].[IX_TabletAccessories_ListingId]', N'IX_TabletAccessorys_ListingId', 'INDEX';

EXEC sp_rename N'[PhoneAccessorys].[IX_PhoneAccessories_ListingId]', N'IX_PhoneAccessorys_ListingId', 'INDEX';

EXEC sp_rename N'[ComputerAccessorys].[IX_ComputerAccessories_ListingId]', N'IX_ComputerAccessorys_ListingId', 'INDEX';

ALTER TABLE [TabletAccessorys] ADD CONSTRAINT [PK_TabletAccessorys] PRIMARY KEY ([Id]);

ALTER TABLE [PhoneAccessorys] ADD CONSTRAINT [PK_PhoneAccessorys] PRIMARY KEY ([Id]);

ALTER TABLE [ComputerAccessorys] ADD CONSTRAINT [PK_ComputerAccessorys] PRIMARY KEY ([Id]);

ALTER TABLE [ComputerAccessorys] ADD CONSTRAINT [FK_ComputerAccessorys_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE;

ALTER TABLE [PhoneAccessorys] ADD CONSTRAINT [FK_PhoneAccessorys_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE;

ALTER TABLE [TabletAccessorys] ADD CONSTRAINT [FK_TabletAccessorys_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250520145921_HaydeHaydeNeWwAY', N'9.0.4');

ALTER TABLE [HeadphonesEarphones] DROP CONSTRAINT [FK_HeadphonesEarphones_Listings_ListingId];

ALTER TABLE [HeadphonesEarphones] DROP CONSTRAINT [PK_HeadphonesEarphones];

EXEC sp_rename N'[HeadphonesEarphones]', N'HeadphoneEarphones', 'OBJECT';

EXEC sp_rename N'[HeadphoneEarphones].[IX_HeadphonesEarphones_ListingId]', N'IX_HeadphoneEarphones_ListingId', 'INDEX';

ALTER TABLE [HeadphoneEarphones] ADD CONSTRAINT [PK_HeadphoneEarphones] PRIMARY KEY ([Id]);

ALTER TABLE [HeadphoneEarphones] ADD CONSTRAINT [FK_HeadphoneEarphones_Listings_ListingId] FOREIGN KEY ([ListingId]) REFERENCES [Listings] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250520162020_ShaneWhoIsShane', N'9.0.4');

ALTER TABLE [Purchases] ADD [CardholderName] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Purchases] ADD [LastFourDigits] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Purchases] ADD [ShippingAddress] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250520183742_NewlyAddedFields', N'9.0.4');

COMMIT;
GO

