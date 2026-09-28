-- =====================================================================
-- SmartRetailPlatform - database script (PostgreSQL)
-- Run top to bottom on an empty database (smart_retail_db).
--
--   Part 0 : Clean start
--   Part 1 : OLTP schema                     (Phase 2)
--   Part 2 : Trigger - automatic stock update (Phase 2)
--   Part 3 : Seed data                        (Phase 2)
--   Part 4 : Star schema + ETL (OLAP)         (Phase 5)
--   Part 5 : Analytical queries               (Phase 5)
--   Part 6 : Optional demos (commented out)
-- =====================================================================


-- ---------------------------------------------------------------------
-- Part 0. Clean start
-- OLAP tables first, then OLTP tables in dependency order
-- (a table can't be dropped while another table still references it).
-- ---------------------------------------------------------------------
DROP TABLE IF EXISTS fact_sales;
DROP TABLE IF EXISTS dim_date;
DROP TABLE IF EXISTS dim_customer;
DROP TABLE IF EXISTS dim_product;
DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS customers;
DROP FUNCTION IF EXISTS reduce_stock_after_order_item();


-- ---------------------------------------------------------------------
-- Part 1. OLTP schema (normalized to 3NF)
-- ---------------------------------------------------------------------
CREATE TABLE customers (
                           id            SERIAL PRIMARY KEY,
                           name          VARCHAR(100) NOT NULL,
                           email         VARCHAR(150) NOT NULL UNIQUE,
                           customer_type VARCHAR(20)  NOT NULL CHECK (customer_type IN ('Regular', 'VIP')),
                           created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE products (
                          id    SERIAL PRIMARY KEY,
                          name  VARCHAR(150)  NOT NULL,
                          price NUMERIC(10,2) NOT NULL CHECK (price >= 0),
                          stock INTEGER       NOT NULL CHECK (stock >= 0)
);

CREATE TABLE orders (
                        id          SERIAL PRIMARY KEY,
                        customer_id INTEGER   NOT NULL REFERENCES customers(id),
                        order_date  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- unit_price is a deliberate denormalization: it freezes the product price
-- at order time, so later price changes never rewrite history.
CREATE TABLE order_items (
                             id         SERIAL PRIMARY KEY,
                             order_id   INTEGER       NOT NULL REFERENCES orders(id),
                             product_id INTEGER       NOT NULL REFERENCES products(id),
                             quantity   INTEGER       NOT NULL CHECK (quantity > 0),
                             unit_price NUMERIC(10,2) NOT NULL
);


-- ---------------------------------------------------------------------
-- Part 2. Trigger: reduce stock automatically after an order item is added
-- The CHECK (stock >= 0) constraint on products is a second safety net.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION reduce_stock_after_order_item()
RETURNS TRIGGER AS $$
BEGIN
UPDATE products
SET stock = stock - NEW.quantity
WHERE id = NEW.product_id;

IF (SELECT stock FROM products WHERE id = NEW.product_id) < 0 THEN
        RAISE EXCEPTION 'Insufficient stock for product id %', NEW.product_id;
END IF;

RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_reduce_stock
    AFTER INSERT ON order_items
    FOR EACH ROW
    EXECUTE FUNCTION reduce_stock_after_order_item();


-- ---------------------------------------------------------------------
-- Part 3. Seed data
-- After a clean start the SERIAL ids are deterministic:
-- Anna = 1, Piotr = 2, Laptop = 1, Wireless Mouse = 2, first order = 1.
-- ---------------------------------------------------------------------
INSERT INTO customers (name, email, customer_type) VALUES
                                                       ('Anna Kowalska', 'anna@example.com', 'Regular'),
                                                       ('Piotr Nowak',   'piotr@example.com', 'VIP');

INSERT INTO products (name, price, stock) VALUES
                                              ('Laptop',         3500.00, 10),
                                              ('Wireless Mouse',  120.00, 50);

-- Sample order: Piotr (VIP) buys 1 Laptop + 2 Wireless Mouse.
-- The trigger reduces stock: Laptop 10 -> 9, Mouse 50 -> 48.
INSERT INTO orders (customer_id) VALUES (2);

INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES
                                                                         (1, 1, 1, 3500.00),
                                                                         (1, 2, 2,  120.00);


-- ---------------------------------------------------------------------
-- Part 4. Star schema (OLAP) + ETL
-- ---------------------------------------------------------------------

-- 4.1 dim_date: pre-computed calendar attributes.
-- The range covers 2026 only - extend it if orders fall outside that year,
-- otherwise the fact_sales foreign key to dim_date will fail.
CREATE TABLE dim_date (
                          date_id      INTEGER PRIMARY KEY,          -- YYYYMMDD, e.g. 20260920
                          full_date    DATE        NOT NULL UNIQUE,
                          day_of_month INTEGER     NOT NULL,
                          day_of_week  VARCHAR(10) NOT NULL,
                          is_weekend   BOOLEAN     NOT NULL,
                          month        INTEGER     NOT NULL,
                          month_name   VARCHAR(20) NOT NULL,
                          quarter      INTEGER     NOT NULL,
                          year         INTEGER     NOT NULL
);

INSERT INTO dim_date (date_id, full_date, day_of_month, day_of_week, is_weekend,
                      month, month_name, quarter, year)
SELECT
    TO_CHAR(d, 'YYYYMMDD')::INTEGER,
    d::DATE,
    EXTRACT(DAY FROM d)::INTEGER,
    TRIM(TO_CHAR(d, 'Day')),
    EXTRACT(ISODOW FROM d) IN (6, 7),
    EXTRACT(MONTH FROM d)::INTEGER,
    TRIM(TO_CHAR(d, 'Month')),
    EXTRACT(QUARTER FROM d)::INTEGER,
    EXTRACT(YEAR FROM d)::INTEGER
FROM generate_series('2026-01-01'::DATE, '2026-12-31'::DATE, '1 day'::INTERVAL) AS d;

-- 4.2 dim_customer: straight copy (extract + load, no transform needed).
CREATE TABLE dim_customer (
                              customer_id   INTEGER PRIMARY KEY,
                              name          VARCHAR(100) NOT NULL,
                              customer_type VARCHAR(20)  NOT NULL
);

INSERT INTO dim_customer (customer_id, name, customer_type)
SELECT id, name, customer_type
FROM customers;

-- 4.3 dim_product: includes a transformed column (price_category).
CREATE TABLE dim_product (
                             product_id     INTEGER PRIMARY KEY,
                             name           VARCHAR(150) NOT NULL,
                             price_category VARCHAR(20)  NOT NULL
);

INSERT INTO dim_product (product_id, name, price_category)
SELECT
    id,
    name,
    CASE
        WHEN price < 200 THEN 'Budget'
        WHEN price BETWEEN 200 AND 2000 THEN 'Mid-range'
        ELSE 'Premium'
        END
FROM products;

-- 4.4 fact_sales: one row per sold order line (the center of the star).
CREATE TABLE fact_sales (
                            fact_id      SERIAL PRIMARY KEY,
                            date_id      INTEGER       NOT NULL REFERENCES dim_date(date_id),
                            customer_id  INTEGER       NOT NULL REFERENCES dim_customer(customer_id),
                            product_id   INTEGER       NOT NULL REFERENCES dim_product(product_id),
                            quantity     INTEGER       NOT NULL,
                            total_amount NUMERIC(10,2) NOT NULL
);

-- ETL (full load): OLTP -> OLAP.
-- Re-running this without emptying fact_sales duplicates the rows:
-- run TRUNCATE fact_sales RESTART IDENTITY; first if you need to reload.
INSERT INTO fact_sales (date_id, customer_id, product_id, quantity, total_amount)
SELECT
    TO_CHAR(o.order_date, 'YYYYMMDD')::INTEGER,
    o.customer_id,
    oi.product_id,
    oi.quantity,
    oi.quantity * oi.unit_price
FROM orders o
         JOIN order_items oi ON oi.order_id = o.id;


-- ---------------------------------------------------------------------
-- Part 5. Analytical queries on the star schema
-- ---------------------------------------------------------------------

-- 5.1 Units sold and revenue per product
SELECT
    dp.name AS product_name,
    dp.price_category,
    SUM(fs.quantity)     AS total_units_sold,
    SUM(fs.total_amount) AS total_revenue
FROM fact_sales fs
         JOIN dim_product dp ON fs.product_id = dp.product_id
GROUP BY dp.name, dp.price_category
ORDER BY total_units_sold DESC;

-- 5.2 Revenue by customer type (VIP vs Regular)
SELECT
    dc.customer_type,
    COUNT(DISTINCT fs.customer_id)    AS unique_customers,
    SUM(fs.total_amount)              AS total_revenue,
    ROUND(AVG(fs.total_amount), 2)    AS avg_sale_amount
FROM fact_sales fs
         JOIN dim_customer dc ON fs.customer_id = dc.customer_id
GROUP BY dc.customer_type;

-- 5.3 Weekend vs weekday sales (possible because is_weekend is pre-computed)
SELECT
    dd.is_weekend,
    COUNT(*)             AS number_of_sales,
    SUM(fs.total_amount) AS total_revenue
FROM fact_sales fs
         JOIN dim_date dd ON fs.date_id = dd.date_id
GROUP BY dd.is_weekend;


-- ---------------------------------------------------------------------
-- Part 6. Optional demos (commented out - remove the leading -- to run)
-- ---------------------------------------------------------------------

-- Demo 1: atomicity. The failing order_items insert (stock too low) rolls
-- back the whole transaction, including the order row that "succeeded".
-- BEGIN;
-- INSERT INTO orders (customer_id) VALUES (1);
-- INSERT INTO order_items (order_id, product_id, quantity, unit_price)
--     VALUES (currval('orders_id_seq'), 2, 999, 120.00);   -- fails
-- ROLLBACK;
-- SELECT * FROM orders;   -- the rolled-back order is not there

-- Demo 2: why unit_price is stored on order_items. After a price change,
-- historical orders keep the price that was paid.
-- UPDATE products SET price = 4200.00 WHERE id = 1;
-- SELECT
--     oi.order_id,
--     p.name        AS product_name,
--     p.price       AS current_product_price,
--     oi.unit_price AS price_at_order_time,
--     oi.quantity
-- FROM order_items oi
-- JOIN products p ON oi.product_id = p.id
-- WHERE p.id = 1;