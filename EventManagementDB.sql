CREATE DATABASE EventManagementDB;
GO
USE EventManagementDB;
GO

-- 1. Bảng Accounts (Tài khoản)
CREATE TABLE Accounts (
    AccountID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Password NVARCHAR(100) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100),
    Role NVARCHAR(20) DEFAULT 'User'
);
GO

-- 2. Bảng Categories (Danh mục sự kiện)
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255)
);
GO

-- 3. Bảng Events (Sự kiện)
CREATE TABLE Events (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Image NVARCHAR(255),
    Location NVARCHAR(200),
    StartDate DATETIME,
    EndDate DATETIME,
    TicketPrice DECIMAL(18,2) DEFAULT 0,
    TotalTickets INT DEFAULT 0,
    CategoryID INT NOT NULL,
    CONSTRAINT FK_Events_Categories FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);
GO

-- 4. Bảng Bookings (Đơn đặt vé)
CREATE TABLE Bookings (
    BookingID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT NOT NULL,
    EventID INT NOT NULL,
    Quantity INT NOT NULL,
    TotalPrice DECIMAL(18,2),
    BookingDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(50) DEFAULT N'Confirmed',
    CONSTRAINT FK_Bookings_Accounts FOREIGN KEY (AccountID) REFERENCES Accounts(AccountID),
    CONSTRAINT FK_Bookings_Events FOREIGN KEY (EventID) REFERENCES Events(EventID)
);
GO

-- Thêm dữ liệu mẫu (Sample Data)
INSERT INTO Accounts (Username, Password, FullName, Email, Role) VALUES
('admin', '123456', N'Quản Trị Viên', 'admin@huflit.edu.vn', 'Admin'),
('user1', '123456', N'Nguyễn Văn A', 'anva@gmail.com', 'User');

INSERT INTO Categories (CategoryName, Description) VALUES
(N'Hội thảo & Công nghệ', N'Các sự kiện về lập trình, AI, công nghệ'),
(N'Âm nhạc & Live Concert', N'Đại nhạc hội, liveshow ca nhạc'),
(N'Thể thao & Giải đấu', N'Giải đấu eSports, chạy bộ, bóng đá');

INSERT INTO Events (Title, Description, Image, Location, StartDate, EndDate, TicketPrice, TotalTickets, CategoryID) VALUES
(N'HUFLIT TechDay 2026', N'Hội thảo công nghệ và triển lãm đồ án sinh viên', 'techday.jpg', N'Cơ sở Hóc Môn', '2026-11-15 08:00:00', '2026-11-15 17:00:00', 0, 500, 1),
(N'Đại Nhạc Hội Sinh Viên 2026', N'Đêm nhạc hội tụ các ca sĩ nổi tiếng', 'music.jpg', N'Sân vận động Quân khu 7', '2026-12-01 18:00:00', '2026-12-01 22:00:00', 150000, 1000, 2);
GO