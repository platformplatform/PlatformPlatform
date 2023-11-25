import { Link } from "react-router-dom";

export default function NotFound() {
  return (
    <div>
      <h2>Not Found</h2>
      <p>Could not find requested resource</p>
      <Link to="/">Return Home</Link>
    </div>
  );
}
