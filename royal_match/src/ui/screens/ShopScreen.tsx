import { useNavigate } from 'react-router-dom';

export function ShopScreen() {
  const navigate = useNavigate();

  return (
    <div className="screen screen--shop">
      <div className="shop__header">
        <button
          type="button"
          className="btn btn--secondary"
          onClick={() => navigate('/main-menu')}
        >
          ← Back
        </button>
        <h2>Shop</h2>
      </div>

      <div className="shop__content">
        <p>Boosters and resources coming soon!</p>
      </div>
    </div>
  );
}
