import React, { useState } from 'react';
import { CreateOrderForm } from './components/CreateOrderForm';
import { OrderList } from './components/OrderList';
import { Order } from './types/order';
import { Button } from './components/ui/button';

function App() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [activeView, setActiveView] = useState<'create' | 'list'>('create');

  const handleOrderCreated = (order: Order) => {
    setOrders(prev => [order, ...prev]);
    // Keep the user on the create form to allow creating multiple orders
  };

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b">
        <div className="container mx-auto px-4 py-4">
          <div className="flex justify-between items-center">
            <h1 className="text-2xl font-bold">Ecommerce Dashboard</h1>
            <nav className="flex gap-2">
              <Button
                variant={activeView === 'create' ? 'default' : 'outline'}
                onClick={() => setActiveView('create')}
              >
                Create Order
              </Button>
              <Button
                variant={activeView === 'list' ? 'default' : 'outline'}
                onClick={() => setActiveView('list')}
              >
                View Orders ({orders.length})
              </Button>
            </nav>
          </div>
        </div>
      </header>

      <main className="container mx-auto px-4 py-8">
        {activeView === 'create' ? (
          <div className="flex justify-center">
            <CreateOrderForm onOrderCreated={handleOrderCreated} />
          </div>
        ) : (
          <OrderList orders={orders} />
        )}
      </main>
    </div>
  );
}

export default App;
