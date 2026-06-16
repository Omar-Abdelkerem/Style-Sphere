-- =============================================
-- StyleSphere Database Creation Script
-- SQL Server Database
-- =============================================

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'StyleSphere')
BEGIN
    CREATE DATABASE StyleSphere;
END
GO

USE StyleSphere;
GO

-- =============================================
-- Create Tables
-- =============================================

-- Table: User
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'User')
BEGIN
    CREATE TABLE [User] (
        UserID INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        Phone NVARCHAR(20) NULL,
        Password NVARCHAR(255) NOT NULL,
        Role NVARCHAR(20) NOT NULL DEFAULT 'Customer'
    );
END
GO

-- Table: Category
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Category')
BEGIN
    CREATE TABLE Category (
        CategoryID INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX) NULL
    );
END
GO

-- Table: Product
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Product')
BEGIN
    CREATE TABLE Product (
        ProductID INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Price DECIMAL(10,2) NOT NULL CHECK (Price > 0),
        Stock INT NOT NULL CHECK (Stock >= 0),
        Image NVARCHAR(255) NULL,
        CategoryID INT NOT NULL,
        FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID) ON DELETE NO ACTION
    );
END
GO

-- Table: Cart
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cart')
BEGIN
    CREATE TABLE Cart (
        CartID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        FOREIGN KEY (UserID) REFERENCES [User](UserID) ON DELETE CASCADE
    );
END
GO

-- Table: CartItem
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItem')
BEGIN
    CREATE TABLE CartItem (
        CartItemID INT IDENTITY(1,1) PRIMARY KEY,
        CartID INT NOT NULL,
        ProductID INT NOT NULL,
        Quantity INT NOT NULL CHECK (Quantity > 0),
        FOREIGN KEY (CartID) REFERENCES Cart(CartID) ON DELETE CASCADE,
        FOREIGN KEY (ProductID) REFERENCES Product(ProductID) ON DELETE NO ACTION
    );
END
GO

-- Table: Order
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Order')
BEGIN
    CREATE TABLE [Order] (
        OrderID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        OrderDate DATE NOT NULL DEFAULT GETDATE(),
        Status NVARCHAR(50) NOT NULL DEFAULT 'Processing',
        TotalAmount DECIMAL(10,2) NOT NULL,
        FOREIGN KEY (UserID) REFERENCES [User](UserID) ON DELETE NO ACTION
    );
END
GO

-- Table: OrderItem
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItem')
BEGIN
    CREATE TABLE OrderItem (
        OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        ProductID INT NOT NULL,
        Quantity INT NOT NULL CHECK (Quantity > 0),
        Price DECIMAL(10,2) NOT NULL,
        FOREIGN KEY (OrderID) REFERENCES [Order](OrderID) ON DELETE CASCADE,
        FOREIGN KEY (ProductID) REFERENCES Product(ProductID) ON DELETE NO ACTION
    );
END
GO

-- Table: Payment
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payment')
BEGIN
    CREATE TABLE Payment (
        PaymentID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        Amount DECIMAL(10,2) NOT NULL,
        PaymentDate DATE NOT NULL DEFAULT GETDATE(),
        Method NVARCHAR(50) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        FOREIGN KEY (OrderID) REFERENCES [Order](OrderID) ON DELETE CASCADE
    );
END
GO

-- Table: Shipper
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Shipper')
BEGIN
    CREATE TABLE Shipper (
        ShipperID INT IDENTITY(1,1) PRIMARY KEY,
        Address NVARCHAR(255) NULL
    );
END
GO

-- Table: Shipment
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Shipment')
BEGIN
    CREATE TABLE Shipment (
        ShipmentID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL,
        ShipperID INT NULL,
        Delivery NVARCHAR(50) NOT NULL DEFAULT 'Processing',
        FOREIGN KEY (OrderID) REFERENCES [Order](OrderID) ON DELETE CASCADE,
        FOREIGN KEY (ShipperID) REFERENCES Shipper(ShipperID) ON DELETE SET NULL
    );
END
GO

-- Table: Review
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Review')
BEGIN
    CREATE TABLE Review (
        ReviewID INT IDENTITY(1,1) PRIMARY KEY,
        UserID INT NOT NULL,
        ProductID INT NOT NULL,
        ReviewDate DATE NOT NULL DEFAULT GETDATE(),
        Comment NVARCHAR(MAX) NULL,
        Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
        FOREIGN KEY (UserID) REFERENCES [User](UserID) ON DELETE NO ACTION,
        FOREIGN KEY (ProductID) REFERENCES Product(ProductID) ON DELETE CASCADE,
        UNIQUE (UserID, ProductID)  -- One review per user per product
    );
END
GO

-- =============================================
-- Insert Sample Data (Optional)
-- =============================================

-- Insert Admin User
IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'admin@stylesphere.com')
BEGIN
    INSERT INTO [User] (Name, Email, Password, Role)
    VALUES ('Admin', 'admin@stylesphere.com', 'admin123', 'Admin');
END
GO

-- Insert Categories
IF NOT EXISTS (SELECT * FROM Category WHERE Name = 'Men')
BEGIN
    INSERT INTO Category (Name, Description) VALUES ('Men', 'Clothing and accessories for men');
END
GO

IF NOT EXISTS (SELECT * FROM Category WHERE Name = 'Women')
BEGIN
    INSERT INTO Category (Name, Description) VALUES ('Women', 'Clothing and accessories for women');
END
GO

IF NOT EXISTS (SELECT * FROM Category WHERE Name = 'Shoes')
BEGIN
    INSERT INTO Category (Name, Description) VALUES ('Shoes', 'Footwear for all occasions');
END
GO

IF NOT EXISTS (SELECT * FROM Category WHERE Name = 'Accessories')
BEGIN
    INSERT INTO Category (Name, Description) VALUES ('Accessories', 'Fashion accessories and add-ons');
END
GO

-- =============================================
-- Insert More Sample Data
-- =============================================

-- Insert More Categories
IF NOT EXISTS (SELECT * FROM Category WHERE Name = 'Kids')
BEGIN
    INSERT INTO Category (Name, Description) VALUES ('Kids', 'Clothing and accessories for children');
END
GO

IF NOT EXISTS (SELECT * FROM Category WHERE Name = 'Electronics')
BEGIN
    INSERT INTO Category (Name, Description) VALUES ('Electronics', 'Electronic devices and gadgets');
END
GO

-- Insert Sample Products (30-40 products)
-- Men's Products
IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Classic White T-Shirt')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Classic White T-Shirt', 'Comfortable cotton t-shirt, perfect for everyday wear', 19.99, 50, 'mens-tshirt-white.jpg', CategoryID FROM Category WHERE Name = 'Men';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Blue Denim Jeans')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Blue Denim Jeans', 'Classic fit denim jeans, durable and stylish', 49.99, 30, 'mens-jeans-blue.jpg', CategoryID FROM Category WHERE Name = 'Men';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Black Leather Jacket')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Black Leather Jacket', 'Genuine leather jacket with zipper closure', 199.99, 15, 'mens-jacket-leather.jpg', CategoryID FROM Category WHERE Name = 'Men';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Navy Blue Blazer')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Navy Blue Blazer', 'Formal blazer for business and special occasions', 129.99, 20, 'mens-blazer-navy.jpg', CategoryID FROM Category WHERE Name = 'Men';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Gray Hoodie')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Gray Hoodie', 'Warm and comfortable hooded sweatshirt', 39.99, 40, 'mens-hoodie-gray.jpg', CategoryID FROM Category WHERE Name = 'Men';
END
GO

-- Women's Products
IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Floral Summer Dress')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Floral Summer Dress', 'Light and airy dress perfect for summer', 59.99, 25, 'womens-dress-floral.jpg', CategoryID FROM Category WHERE Name = 'Women';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Black High Heels')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Black High Heels', 'Elegant black heels for formal occasions', 79.99, 18, 'womens-heels-black.jpg', CategoryID FROM Category WHERE Name = 'Women';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Red Handbag')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Red Handbag', 'Stylish red leather handbag with shoulder strap', 89.99, 12, 'womens-bag-red.jpg', CategoryID FROM Category WHERE Name = 'Women';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'White Blouse')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'White Blouse', 'Classic white blouse for office wear', 34.99, 35, 'womens-blouse-white.jpg', CategoryID FROM Category WHERE Name = 'Women';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Denim Skirt')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Denim Skirt', 'Casual denim skirt, perfect for everyday', 29.99, 28, 'womens-skirt-denim.jpg', CategoryID FROM Category WHERE Name = 'Women';
END
GO

-- Shoes
IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Running Shoes')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Running Shoes', 'Comfortable athletic shoes for running and sports', 69.99, 45, 'shoes-running.jpg', CategoryID FROM Category WHERE Name = 'Shoes';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Brown Leather Boots')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Brown Leather Boots', 'Durable leather boots for winter', 99.99, 22, 'shoes-boots-brown.jpg', CategoryID FROM Category WHERE Name = 'Shoes';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'White Sneakers')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'White Sneakers', 'Classic white sneakers, versatile and stylish', 54.99, 38, 'shoes-sneakers-white.jpg', CategoryID FROM Category WHERE Name = 'Shoes';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Black Dress Shoes')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Black Dress Shoes', 'Formal black shoes for business attire', 89.99, 20, 'shoes-dress-black.jpg', CategoryID FROM Category WHERE Name = 'Shoes';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Sandals')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Sandals', 'Comfortable sandals for summer', 24.99, 50, 'shoes-sandals.jpg', CategoryID FROM Category WHERE Name = 'Shoes';
END
GO

-- Accessories
IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Leather Belt')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Leather Belt', 'Genuine leather belt with silver buckle', 29.99, 40, 'accessory-belt-leather.jpg', CategoryID FROM Category WHERE Name = 'Accessories';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Sunglasses')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Sunglasses', 'UV protection sunglasses with stylish frame', 39.99, 30, 'accessory-sunglasses.jpg', CategoryID FROM Category WHERE Name = 'Accessories';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Wristwatch')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Wristwatch', 'Elegant wristwatch with leather strap', 79.99, 15, 'accessory-watch.jpg', CategoryID FROM Category WHERE Name = 'Accessories';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Baseball Cap')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Baseball Cap', 'Adjustable baseball cap, one size fits all', 19.99, 60, 'accessory-cap.jpg', CategoryID FROM Category WHERE Name = 'Accessories';
END
GO

IF NOT EXISTS (SELECT * FROM Product WHERE Name = 'Scarf')
BEGIN
    INSERT INTO Product (Name, Description, Price, Stock, Image, CategoryID)
    SELECT 'Scarf', 'Warm woolen scarf for winter', 24.99, 35, 'accessory-scarf.jpg', CategoryID FROM Category WHERE Name = 'Accessories';
END
GO

-- Insert Sample Customers (10-15 users)
IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'john.doe@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('John Doe', 'john.doe@email.com', '555-0101', 'password123', 'Customer');
END
GO

IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'jane.smith@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('Jane Smith', 'jane.smith@email.com', '555-0102', 'password123', 'Customer');
END
GO

IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'mike.johnson@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('Mike Johnson', 'mike.johnson@email.com', '555-0103', 'password123', 'Customer');
END
GO

IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'sarah.williams@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('Sarah Williams', 'sarah.williams@email.com', '555-0104', 'password123', 'Customer');
END
GO

IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'david.brown@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('David Brown', 'david.brown@email.com', '555-0105', 'password123', 'Customer');
END
GO

IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'emily.davis@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('Emily Davis', 'emily.davis@email.com', '555-0106', 'password123', 'Customer');
END
GO

IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'chris.miller@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('Chris Miller', 'chris.miller@email.com', '555-0107', 'password123', 'Customer');
END
GO

IF NOT EXISTS (SELECT * FROM [User] WHERE Email = 'lisa.wilson@email.com')
BEGIN
    INSERT INTO [User] (Name, Email, Phone, Password, Role)
    VALUES ('Lisa Wilson', 'lisa.wilson@email.com', '555-0108', 'password123', 'Customer');
END
GO

-- Insert Sample Orders
-- Order 1
IF EXISTS (SELECT * FROM [User] WHERE Email = 'john.doe@email.com')
    AND EXISTS (SELECT * FROM Product WHERE Name = 'Classic White T-Shirt')
    AND EXISTS (SELECT * FROM Product WHERE Name = 'Blue Denim Jeans')
BEGIN
    DECLARE @Order1ID INT;
    INSERT INTO [Order] (UserID, OrderDate, Status, TotalAmount)
    SELECT UserID, DATEADD(day, -10, GETDATE()), 'Completed', 69.98
    FROM [User] WHERE Email = 'john.doe@email.com';
    SET @Order1ID = SCOPE_IDENTITY();
    
    INSERT INTO OrderItem (OrderID, ProductID, Quantity, Price)
    SELECT @Order1ID, ProductID, 2, 19.99 FROM Product WHERE Name = 'Classic White T-Shirt';
    
    INSERT INTO OrderItem (OrderID, ProductID, Quantity, Price)
    SELECT @Order1ID, ProductID, 1, 49.99 FROM Product WHERE Name = 'Blue Denim Jeans';
    
    INSERT INTO Payment (OrderID, Amount, PaymentDate, Method, Status)
    VALUES (@Order1ID, 69.98, DATEADD(day, -10, GETDATE()), 'Credit Card', 'Completed');
END
GO

-- Order 2
IF EXISTS (SELECT * FROM [User] WHERE Email = 'jane.smith@email.com')
    AND EXISTS (SELECT * FROM Product WHERE Name = 'Running Shoes')
BEGIN
    DECLARE @Order2ID INT;
    INSERT INTO [Order] (UserID, OrderDate, Status, TotalAmount)
    SELECT UserID, DATEADD(day, -5, GETDATE()), 'Processing', 69.99
    FROM [User] WHERE Email = 'jane.smith@email.com';
    SET @Order2ID = SCOPE_IDENTITY();
    
    INSERT INTO OrderItem (OrderID, ProductID, Quantity, Price)
    SELECT @Order2ID, ProductID, 1, 69.99 FROM Product WHERE Name = 'Running Shoes';
    
    INSERT INTO Payment (OrderID, Amount, PaymentDate, Method, Status)
    VALUES (@Order2ID, 69.99, DATEADD(day, -5, GETDATE()), 'Debit Card', 'Pending');
END
GO

-- Insert Sample Reviews
IF EXISTS (SELECT * FROM [User] WHERE Email = 'john.doe@email.com')
    AND EXISTS (SELECT * FROM Product WHERE Name = 'Classic White T-Shirt')
    AND NOT EXISTS (SELECT * FROM Review WHERE UserID = (SELECT UserID FROM [User] WHERE Email = 'john.doe@email.com') 
                    AND ProductID = (SELECT ProductID FROM Product WHERE Name = 'Classic White T-Shirt'))
BEGIN
    INSERT INTO Review (UserID, ProductID, ReviewDate, Comment, Rating)
    SELECT UserID, ProductID, DATEADD(day, -8, GETDATE()), 'Great quality t-shirt, very comfortable!', 5
    FROM [User] CROSS JOIN Product
    WHERE [User].Email = 'john.doe@email.com' AND Product.Name = 'Classic White T-Shirt';
END
GO

IF EXISTS (SELECT * FROM [User] WHERE Email = 'jane.smith@email.com')
    AND EXISTS (SELECT * FROM Product WHERE Name = 'Running Shoes')
    AND NOT EXISTS (SELECT * FROM Review WHERE UserID = (SELECT UserID FROM [User] WHERE Email = 'jane.smith@email.com') 
                    AND ProductID = (SELECT ProductID FROM Product WHERE Name = 'Running Shoes'))
BEGIN
    INSERT INTO Review (UserID, ProductID, ReviewDate, Comment, Rating)
    SELECT UserID, ProductID, DATEADD(day, -3, GETDATE()), 'Perfect for running, very comfortable.', 4
    FROM [User] CROSS JOIN Product
    WHERE [User].Email = 'jane.smith@email.com' AND Product.Name = 'Running Shoes';
END
GO

-- =============================================
-- Script Complete
-- =============================================
PRINT 'Database StyleSphere created successfully!';
PRINT 'Tables created: User, Category, Product, Cart, CartItem, Order, OrderItem, Payment, Shipper, Shipment, Review';
PRINT 'Sample data inserted:';
PRINT '  - 1 Admin user';
PRINT '  - 8 Customer users';
PRINT '  - 6 Categories';
PRINT '  - 20 Products';
PRINT '  - 2 Orders with order items';
PRINT '  - 2 Payments';
PRINT '  - 2 Reviews';
GO

